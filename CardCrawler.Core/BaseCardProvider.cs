using CardCrawler.Core.Interfaces;
using CardCrawler.Core.Models;
using System.Diagnostics;
using System.Text.Json;

namespace CardCrawler.Core
{
    public abstract class BaseCardProvider : ICardDataProvider
    {
        public abstract string SourceName { get; }
        protected Dictionary<string, decimal?> _pricesByName = [];
        protected readonly string _cacheFile;

        protected BaseCardProvider(string cacheFileName)
        {
            _cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cacheFileName);
        }

        public abstract Task<CardData?> GetCardData(string cardName);

        public virtual Task<bool> CheckConnection()
        {
            return Task.FromResult(_pricesByName.Count > 0);
        }

        public abstract Task UpdateLocalCardData();

        public virtual async Task InitializeAsync()
        {
            if (File.Exists(_cacheFile))
            {
                try
                {
                    string content = await File.ReadAllTextAsync(_cacheFile);
                    List<CachedCard>? data = null;

                    try
                    {
                        data = JsonSerializer.Deserialize<List<CachedCard>>(content);
                    }
                    catch
                    {
                        // Fallback: Try parsing compact JSON array format [["Name", price], ...]
                        using var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            data = [];
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                if (elem.ValueKind == JsonValueKind.Array && elem.GetArrayLength() >= 2)
                                {
                                    string? name = elem[0].GetString();
                                    if (!string.IsNullOrWhiteSpace(name) && elem[1].TryGetDecimal(out decimal price))
                                    {
                                        data.Add(new CachedCard("", name, price));
                                    }
                                }
                            }
                        }
                    }

                    if (data != null)
                    {
                        var validData = data.Where(c => !string.IsNullOrWhiteSpace(c.Name));

                        // Handle potential duplicates by taking the minimum price or first encounter
                        _pricesByName = validData
                            .Where(c => c.Price.HasValue && c.Price.Value > 0) // Only consider entries with valid prices
                            .GroupBy(c => c.Name.ToLowerInvariant().Trim())
                            .ToDictionary(g => g.Key, g => g.Min(c => c.Price));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading {SourceName} cache: {ex.Message}");
                }
            }
        }
    }
}
