using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

using CardCrawler.Core;
using CardCrawler.Core.Models;

namespace CardCrawler.Scryfall
{
    public class ScryfallProvider : BaseCardProvider
    {
        public override string SourceName => "Scryfall";
        
        // Static HttpClient to avoid socket exhaustion
        private static readonly HttpClient _client = new()
        {
            BaseAddress = new Uri("https://api.scryfall.com/")
        };

        static ScryfallProvider()
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "CardCrawler/1.0");
            _client.DefaultRequestHeaders.Add("Accept", "application/json;q=0.9,*/*;q=0.8");
        }

        public ScryfallProvider() : base("scryfall_prices.json")
        {
        }

        public override async Task UpdateLocalCardData()
        {
            var dInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            var files = dInfo.GetFiles("all-cards-*.json");
            if (files.Length == 0)
            {
                return;
            }

            List<CachedCard> cards = [];
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };

            string file = files.First().FullName;

            try
            {
                using FileStream stream = File.OpenRead(file);
                await foreach (System.Text.Json.Nodes.JsonObject? jsonNode in JsonSerializer.DeserializeAsyncEnumerable<System.Text.Json.Nodes.JsonObject>(stream, options))
                {
                    if (jsonNode == null)
                    {
                        continue;
                    }
                    string? id = jsonNode["id"]?.GetValue<string>();
                    string? name = jsonNode["name"]?.GetValue<string>();

                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    decimal price = -1;
                    System.Text.Json.Nodes.JsonNode? pricesNode = jsonNode["prices"];
                    if (pricesNode is not null)
                    {
                        string? eurStr = pricesNode["eur"]?.GetValue<string>();
                        if (eurStr != null)
                        {
                            decimal.TryParse(eurStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out price);
                        }
                    }
                    cards.Add(new CachedCard(id, name, price));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Scryfall data: {ex.Message}");
            }

            await File.WriteAllTextAsync(_cacheFile, JsonSerializer.Serialize(cards));
        }

        public DateTime LastRequest = DateTime.MinValue;
        
        public override async Task<CardData?> GetCardData(string cardName)
        {
            if (string.IsNullOrWhiteSpace(cardName))
            {
                return null;
            }

            // Fast local lookup
            string key = cardName.ToLowerInvariant().Trim();
            if (_pricesByName.TryGetValue(key, out decimal? cachedPrice))
            {
                return new CardData(cardName)
                {
                    PriceTrend = cachedPrice,
                    Url = $"https://scryfall.com/search?q=cards/search?q=!\"{HttpUtility.UrlEncode($"{cardName}\" prefer:eur-low")}"
                };
            }

            DateTime nextRequest = LastRequest + TimeSpan.FromMilliseconds(100);
            double wait = (nextRequest - DateTime.Now).TotalMilliseconds;
            if (wait > 0)
            {
                await Task.Delay((int)wait);
            }

            HttpResponseMessage? response = await _client.GetAsync($"cards/search?q=!\"{HttpUtility.UrlEncode($"{cardName}\" prefer:eur-low")} ");

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

            LastRequest = DateTime.Now;

            return card;
        }
    }
}
