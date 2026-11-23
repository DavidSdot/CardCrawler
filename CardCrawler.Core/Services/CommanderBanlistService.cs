using System.Text.Json;
using System.Text.Json.Nodes;

namespace CardCrawler.Core.Services
{
    public static class CommanderBanlistService
    {
        private const string ScryfallBanlistUrl = "https://api.scryfall.com/cards/search?q=banned%3Acommander";
        public const string BanlistFileName = "commander_banlist.json";

        private static readonly HttpClient _client = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        static CommanderBanlistService()
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "CardCrawler/1.0");
            _client.DefaultRequestHeaders.Add("Accept", "application/json;q=0.9,*/*;q=0.8");
        }

        /// <summary>
        /// Fetches the current official Commander banlist from Scryfall (which mirrors WotC's official list).
        /// Falls back to a local Scryfall bulk file (all-cards-*.json) or cached commander_banlist.json if offline.
        /// </summary>
        public static async Task<List<string>> FetchBannedCardsAsync(string? localDirectory = null)
        {
            List<string> apiResult = await TryFetchFromScryfallApiAsync();
            if (apiResult.Count > 0)
            {
                return apiResult;
            }

            string dir = localDirectory ?? Directory.GetCurrentDirectory();
            List<string> bulkResult = await TryReadFromScryfallBulkFileAsync(dir);
            if (bulkResult.Count > 0)
            {
                return bulkResult;
            }

            return await TryReadCachedBanlistAsync(dir);
        }

        public static async Task SaveBanlistJsonAsync(string directory, IReadOnlyCollection<string> bannedCards)
        {
            if (bannedCards.Count == 0)
            {
                return;
            }

            string path = Path.Combine(directory, BanlistFileName);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(bannedCards));
        }

        private static async Task<List<string>> TryFetchFromScryfallApiAsync()
        {
            HashSet<string> banned = new(StringComparer.OrdinalIgnoreCase);
            string? nextUrl = ScryfallBanlistUrl;

            try
            {
                while (!string.IsNullOrWhiteSpace(nextUrl))
                {
                    using HttpResponseMessage response = await _client.GetAsync(nextUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Warning: Scryfall banlist request failed with HTTP {(int)response.StatusCode}.");
                        return [];
                    }

                    await using Stream stream = await response.Content.ReadAsStreamAsync();
                    using JsonDocument doc = await JsonDocument.ParseAsync(stream);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("data", out JsonElement dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement card in dataArray.EnumerateArray())
                        {
                            if (card.TryGetProperty("name", out JsonElement nameEl))
                            {
                                string? name = nameEl.GetString()?.Trim();
                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    banned.Add(name.ToLowerInvariant());
                                }
                            }
                        }
                    }

                    if (root.TryGetProperty("has_more", out JsonElement hasMoreEl) &&
                        hasMoreEl.ValueKind == JsonValueKind.True &&
                        root.TryGetProperty("next_page", out JsonElement nextPageEl))
                    {
                        nextUrl = nextPageEl.GetString();
                        await Task.Delay(100);
                    }
                    else
                    {
                        nextUrl = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not fetch Commander banlist from Scryfall API: {ex.Message}");
                return [];
            }

            return [.. banned.OrderBy(n => n, StringComparer.Ordinal)];
        }

        private static async Task<List<string>> TryReadFromScryfallBulkFileAsync(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return [];
            }

            FileInfo? bulkFile = new DirectoryInfo(directory)
                .GetFiles("all-cards-*.json")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (bulkFile == null)
            {
                return [];
            }

            HashSet<string> banned = new(StringComparer.OrdinalIgnoreCase);
            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

            try
            {
                Console.WriteLine($"Extracting Commander banlist from {bulkFile.Name}...");
                using FileStream stream = bulkFile.OpenRead();
                await foreach (JsonObject? jsonNode in JsonSerializer.DeserializeAsyncEnumerable<JsonObject>(stream, options))
                {
                    if (jsonNode == null)
                    {
                        continue;
                    }

                    string? commanderLegality = jsonNode["legalities"]?["commander"]?.GetValue<string>();
                    if (!string.Equals(commanderLegality, "banned", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string? name = jsonNode["name"]?.GetValue<string>()?.Trim();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        banned.Add(name.ToLowerInvariant());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error reading Scryfall bulk file for banlist: {ex.Message}");
                return [];
            }

            return [.. banned.OrderBy(n => n, StringComparer.Ordinal)];
        }

        private static async Task<List<string>> TryReadCachedBanlistAsync(string directory)
        {
            string[] candidatePaths =
            [
                Path.Combine(directory, BanlistFileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BanlistFileName)
            ];

            foreach (string path in candidatePaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    string json = await File.ReadAllTextAsync(path);
                    List<string>? cached = JsonSerializer.Deserialize<List<string>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        return [.. cached
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .Select(n => n.Trim().ToLowerInvariant())
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(n => n, StringComparer.Ordinal)];
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not read cached banlist from {path}: {ex.Message}");
                }
            }

            return [];
        }
    }
}
