using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

using CardCrawler.Core.Models;

namespace CardCrawler.Scryfall
{
    public static class Api
    {

        public static async Task UpdateLocalCardData(string file)
        {
            if (!File.Exists(file))
            {
                return;
            }

            List<CachedCard> cards = [];
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                using FileStream stream = File.OpenRead(file);
                await foreach (JsonObject? jsonNode in JsonSerializer.DeserializeAsyncEnumerable<JsonObject>(stream, options))
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

                    decimal price = 0;
                    JsonNode? pricesNode = jsonNode["prices"];
                    if (pricesNode is not null)
                    {
                        string? eurStr = pricesNode["eur"]?.GetValue<string>();
                        if (eurStr != null)
                        {
                            decimal.TryParse(eurStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out price);
                        }
                    }

                    if (price > 0)
                    {
                        cards.Add(new CachedCard(id, name, price));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Scryfall data: {ex.Message}");
            }

            string cachePath = Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, "scryfall_prices.json");
            await File.WriteAllTextAsync(cachePath, JsonSerializer.Serialize(cards));

        }
    }

    public record CachedCard(string Id, string Name, decimal Price);
}
