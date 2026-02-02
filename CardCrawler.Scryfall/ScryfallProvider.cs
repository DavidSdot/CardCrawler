using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

using CardCrawler.Core.Interfaces;
using CardCrawler.Core.Models;

namespace CardCrawler.Scryfall
{
    public class ScryfallProvider : ICardDataProvider
    {
        public string SourceName => "Scryfall";
        private Dictionary<string, decimal> _prices = [];
        private Dictionary<string, decimal> _pricesByName = [];
        private readonly string _cacheFile;

        public ScryfallProvider(string cacheFile)
        {
            _cacheFile = cacheFile;
        }

        public async Task InitializeAsync()
        {
            if (File.Exists(_cacheFile))
            {
                try
                {
                    string content = await File.ReadAllTextAsync(_cacheFile);
                    // Try to deserialize as List<CachedCard>
                    List<CachedCard>? data = JsonSerializer.Deserialize<List<CachedCard>>(content);
                    if (data != null)
                    {
                        _prices = data.ToDictionary(k => k.Id, v => v.Price);
                        // Group by name and pick the lowest price for duplicates? Or just overwrite?
                        // User asked for "lowest price" in search, but cache structure is just "one entry per card object".
                        // If multiple prints exist, we have multiple entries.
                        // For name lookup, we want the lowest price across all prints with that name.
                        _pricesByName = data
                            .GroupBy(c => c.Name.ToLowerInvariant().Trim())
                            .ToDictionary(g => g.Key, g => g.Min(c => c.Price));
                    }
                }
                catch (Exception)
                {
                    // Ignore load errors
                }
            }
        }

        public Task<bool> CheckConnection()
        {
            return Task.FromResult(_prices.Count > 0);
        }

        public DateTime LastRequest = DateTime.MinValue;
        public async Task<CardData?> GetCardData(string cardName)
        {
            if (string.IsNullOrWhiteSpace(cardName))
            {
                return null;
            }

            // Fast local lookup
            string key = cardName.ToLowerInvariant().Trim();
            if (_pricesByName.TryGetValue(key, out decimal cachedPrice))
            {
                // Create a minimal card data from cache
                // Note: valid URL is harder without storing it, but we can generate a search URL
                return new CardData(cardName)
                {
                    PriceTrend = cachedPrice,
                    Url = $"https://scryfall.com/search?q={System.Web.HttpUtility.UrlEncode(cardName)}&order=eur"
                };
            }

            // Fallback to API if not in cache (optional, but requested in previous steps?)
            // Actually, offline provider usually implies NO api calls, but current code mixed them.
            // Let's keep API fallback for "not found locally" or just return null?
            // User requested "UpdateLocalCardData stores ID/Name/Price", implying reliance on local data.
            // BUT, if we want to be fully offline, we return here.
            // Given the previous code had API calls, I should probably keep them OR check if "offline mode" is stricter.
            // Let's assume we use local if found, else API.

            HttpClient client = new()
            {
                BaseAddress = new Uri("https://api.scryfall.com/")
            };
            client.DefaultRequestHeaders.Add("User-Agent", "CardCrawler/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json;q=0.9,*/*;q=0.8");


            DateTime nextRequest = LastRequest + TimeSpan.FromMilliseconds(100);
            double wait = (nextRequest - DateTime.Now).TotalMilliseconds;
            if (wait > 0)
            {
                await Task.Delay((int)wait);
            }

            HttpResponseMessage? response = await client.GetAsync($"cards/search?q={HttpUtility.UrlEncode($"{cardName} prefer:eur-low")} ");

            if (response is null || !response.IsSuccessStatusCode)
            {
                return null;
            }

            JsonDocument? json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            if (json is null || !json.RootElement.TryGetProperty("data", out JsonElement dataArray) || dataArray.GetArrayLength() == 0)
            {
                return null;
            }

            JsonElement element = dataArray[0];

            cardName = element.GetProperty("name").GetString() ?? cardName;
            string id = element.GetProperty("id").GetString() ?? string.Empty;
            string priceStr = "";

            if (element.TryGetProperty("prices", out JsonElement pricesEl) && pricesEl.TryGetProperty("eur", out JsonElement eurEl))
            {
                priceStr = eurEl.GetString() ?? "";
            }

            string url = element.GetProperty("scryfall_uri").GetString() ?? string.Empty;

            decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal eurPrice);

            CardData card = new(cardName)
            {
                Url = url,
                PriceTrend = eurPrice
            };

            return card;
        }
    }
}
