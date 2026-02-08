using CardCrawler.Cardmarket;
using CardCrawler.Core.Interfaces;
using CardCrawler.Core.Models;
using CardCrawler.Scryfall;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardCrawler
{
    internal class Program
    {
        private static readonly List<string> BasicLands = ["Forest", "Island", "Mountain", "Plains", "Swamp"];
        private static readonly List<string> ExcludedCards = [];

        private static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();
#if !DEBUG
            ConsoleUi.ShowBanner();
#endif
            CrawlerOptions? options = ArgumentParser.Parse(args);
            if (options == null)
            {
                // ArgumentParser handles printing usage/errors for us (mostly) but if it returns null without having printed usage for help...
                // Actually ArgumentParser prints usage if unnamed.Count == 0 and not update mode.
                return;
            }

            // 1. Initialize Provider
            ICardDataProvider provider;

            // Allow fallback if needed, but ScryfallProvider handles missing file by just not loading cache.

            if (options.DataSource == "cardmarket")
            {
                provider = new CardMarketProvider();
            }
            else
            {
                provider = new ScryfallProvider();
            }

            // 2. Handle Cache Update Mode
            if (options.UpdateCache)
            {
                Console.WriteLine($"Updating local card data from {provider.SourceName}...");
                await provider.UpdateLocalCardData();
                Console.WriteLine("Done.");
                return;
            }

            // 3. Initialize Provider (Load Cache)
            await provider.InitializeAsync();
            Console.WriteLine($"Using {provider.SourceName} as data source.\r\n");

            // 4. Check Connection (Only if actually fetching prices)
            await CheckCardSource(provider);

            // 5. Read Input File
            List<string> lines = [];
            if (options.InputPath != null)
            {
                if (File.Exists(options.InputPath))
                {
                    lines = [.. File.ReadLines(options.InputPath).Where(l => !string.IsNullOrWhiteSpace(l))];
                }
                else
                {
                    Console.WriteLine($"Input file not found: {options.InputPath}");
                    return;
                }

                if (lines.Count == 0)
                {
                    Console.WriteLine("No cards found in input file.");
                    return;
                }
            }
            else
            {
                // Should have been caught by ArgumentParser but just in case
                Console.WriteLine("No input file specified.");
                return;
            }

            // 6. Handle Exclusions
            if (!string.IsNullOrWhiteSpace(options.ExcludeFile) && File.Exists(options.ExcludeFile))
            {
                foreach (string name in File.ReadLines(options.ExcludeFile).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    ExcludedCards.Add(Utilities.CleanCardName(name));
                }
            }

            if (options.ExcludeFirst)
            {
                Console.WriteLine("Excluding first card from total");
            }
            if (options.ExcludeBasics)
            {
                Console.WriteLine("Excluding basic lands from total");
            }
            if (ExcludedCards.Count > 0)
            {
                Console.WriteLine($"Excluding {ExcludedCards.Count} cards from total");
            }
            Console.WriteLine("\r\n");

            // 7. Process Cards
            List<StatusEntry> statusList = [.. lines.Select(n => new StatusEntry(n))];
            decimal total = 0M;

            int headerLine = Console.CursorTop;
            Console.WriteLine();
            int statusLine = Console.CursorTop;
            Console.WriteLine();
            int resultLine = Console.CursorTop;
            Console.WriteLine();

            for (int i = 0; i < lines.Count; i++)
            {
                string original = lines[i];
                (int count, string cleanName) = Utilities.ParseCardLine(original);

                ConsoleUi.UpdateProgress(i, lines.Count, total, options.BudgetLimit, headerLine, cleanName, statusLine);

                CardData? card = await provider.GetCardData(cleanName);

                bool isBasic = BasicLands.Contains(cleanName);
                bool isExcluded = ExcludedCards.Contains(cleanName);
                bool include = !((options.ExcludeBasics && isBasic) || isExcluded);

                if (i == 0 && options.ExcludeFirst)
                {
                    include = false;
                }

                string sym, info = "";
                if (card is not null)
                {
                    statusList[i] = new(card.Name) { Count = count };

                    sym = include ? "✔" : "~";
                    decimal rowTotal = card.PriceTrend ?? 0 * count;

                    statusList[i].UnitPrice = card.PriceTrend ?? 0;
                    statusList[i].TotalPrice = rowTotal;

                    if (options.PriceLimit > 0 && card.PriceTrend > options.PriceLimit)
                    {
                        statusList[i].ExceedsLimit = true;
                    }

                    if (options.ExcludeBasics || ExcludedCards.Count > 0)
                    {
                        info = include ? "included" : "excluded";
                        if (include)
                        {
                            total += rowTotal;
                        }
                    }
                    else
                    {
                        total += rowTotal;
                    }

                }
                else
                {
                    sym = "✖";
                    info = "not found";
                    statusList[i] = new StatusEntry(cleanName) { Count = count };
                }
                statusList[i].Symbol = sym;
                statusList[i].Info = info;

                ConsoleUi.PrintStatusResult(statusList[i], resultLine);

                await Task.Delay(25);
            }

            Console.Clear();
#if !DEBUG
            ConsoleUi.ShowBanner();
#endif
            ConsoleUi.DrawTable(statusList, options, total);

            if (!string.IsNullOrWhiteSpace(options.OutputPath))
            {
                await CsvExporter.SaveCsvAsync(options.OutputPath, statusList, total, options.BudgetLimit);
                Console.WriteLine($"\nSaved CSV to {options.OutputPath}");
            }
        }

        private static async Task CheckCardSource(ICardDataProvider provider)
        {
            Console.Write($"Checking {provider.SourceName}... ");
            while (!await provider.CheckConnection())
            {
                Console.WriteLine("FAILED, retrying...");
                await Task.Delay(2000);
                Console.Write($"Checking {provider.SourceName}... ");
            }
            Console.WriteLine("OK\r\n");
        }
    }
}
