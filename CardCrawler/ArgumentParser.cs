using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CardCrawler
{
    public record CrawlerOptions
    {
        public string DataSource { get; init; } = "scryfall";
        public string? InputPath { get; init; }
        public string? OutputPath { get; init; }
        public string? ExcludeFile { get; init; }
        public bool ExcludeFirst { get; init; }
        public bool ExcludeBasics { get; init; }
        public decimal BudgetLimit { get; init; } = -1M;
        public decimal PriceLimit { get; init; } = -1M;
        public bool UpdateCache { get; init; }
        public string? UpdateCacheFile { get; init; }
        public List<string> UnnamedArgs { get; init; } = [];
    }

    public static class ArgumentParser
    {
        public static CrawlerOptions? Parse(string[] args)
        {
            List<string> unnamed = [];
            bool showHelp = false;

            // Mutable temporary state for parsing
            string dataSource = "scryfall";
            string? excludeFile = null;
            bool excludeFirst = false;
            bool excludeBasics = false;
            decimal budgetLimit = -1M;
            decimal priceLimit = -1M;
            bool updateCache = false;
            
            foreach (string arg in args)
            {
                if (arg.StartsWith("--datasource=", StringComparison.Ordinal))
                {
                    dataSource = arg["--datasource=".Length..].ToLowerInvariant();
                    if (dataSource != "scryfall" && dataSource != "cardmarket")
                    {
                        Console.WriteLine($"Unknown datasource: {dataSource}");
                        return null;
                    }
                }
                else if (arg.StartsWith("--exclude=", StringComparison.Ordinal))
                {
                    excludeFile = arg["--exclude=".Length..];
                }
                else if (arg.Equals("--excludeFirst", StringComparison.Ordinal))
                {
                    excludeFirst = true;
                }
                else if (arg.Equals("--no-basics", StringComparison.Ordinal))
                {
                    excludeBasics = true;
                }
                else if (arg.StartsWith("--budget=", StringComparison.Ordinal) &&
                         decimal.TryParse(arg.AsSpan("--budget=".Length), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal b))
                {
                    budgetLimit = b;
                }
                else if ((arg.StartsWith("--limit=", StringComparison.Ordinal) || arg.StartsWith("--priceLimit=", StringComparison.Ordinal)))
                {
                    string val = arg.Contains('=') ? arg[(arg.IndexOf('=') + 1)..] : "";
                     if(decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal l))
                     {
                         priceLimit = l;
                     }
                }
                else if (arg.Equals("--help", StringComparison.Ordinal) || arg.Equals("-h", StringComparison.Ordinal))
                {
                    showHelp = true;
                }
                else if (arg.Equals("--update-cache", StringComparison.Ordinal))
                {
                    updateCache = true;
                }
                else if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    Console.WriteLine($"Unknown option: {arg}");
                    return null;
                }
                else
                {
                    unnamed.Add(arg);
                }
            }

            if (!updateCache && (unnamed.Count == 0 || showHelp))
            {
                PrintUsage();
                return null;
            }

            string? inputPath = null;
            string? outputPath = null;

            if (!updateCache && unnamed.Count > 0)
            {
                inputPath = unnamed[0];
                if (unnamed.Count > 1)
                {
                    outputPath = unnamed[1];
                }
            }

            return new CrawlerOptions
            {
                DataSource = dataSource,
                InputPath = inputPath,
                OutputPath = outputPath,
                ExcludeFile = excludeFile,
                ExcludeFirst = excludeFirst,
                ExcludeBasics = excludeBasics,
                BudgetLimit = budgetLimit,
                PriceLimit = priceLimit,
                UpdateCache = updateCache,
                UpdateCacheFile = unnamed.FirstOrDefault(),
                UnnamedArgs = unnamed
            };
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  CardCrawler <input-file> [output-file] [options]");
            Console.WriteLine();
            Console.WriteLine("Description:");
            Console.WriteLine("  Fetches price data for a list of Magic cards from Cardmarket,");
            Console.WriteLine("  shows a live progress indicator, and at the end outputs a summary");
            Console.WriteLine("  and (optionally) a CSV file.");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <input-file>           Path to a text file containing one card per line.");
            Console.WriteLine("  [output-file]          (Optional) Path to save results as CSV.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --datasource=<source>  Datasource scrfall or cardmarket (default is scryfall),");
            Console.WriteLine("  --excludeFirst         Exclude the first card from from total (eg. your commander).");
            Console.WriteLine("  --exclude=<file>       Path to a text file with card names to exclude");
            Console.WriteLine("                         (one name per line). These cards will be skipped");
            Console.WriteLine("                         in the total calculation.");
            Console.WriteLine("  --no-basics            Exclude basic lands (Forest, Island, etc.) from total.");
            Console.WriteLine("  --budget=<amount>      Set custom budget limit in EUR (default: 20.00).");
            Console.WriteLine("  --limit=<amount>       Set custom price limit per card in EUR. Cards exceeding");
            Console.WriteLine("                         this limit will be highlighted.");
            Console.WriteLine("  --update-cache         Updates the local price data for <datasource> from bulk json.");
            Console.WriteLine("                         cardmarket: price_guide_1.json and products_singles_1.json");
            Console.WriteLine("                         scyfall: ex. all-cards-2026...........json ");
            Console.WriteLine("  -h, --help             Show this help message and exit.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  CardCrawler deck.txt");
            Console.WriteLine("  CardCrawler deck.txt prices.csv --budget=30.00");
            Console.WriteLine("  CardCrawler deck.txt prices.csv --limit=5.00");
        }
    }
}
