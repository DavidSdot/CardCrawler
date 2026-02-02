using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CardCrawler.Cardmarket;
using CardCrawler.Core.Models;
using CardCrawler.Scryfall;

namespace CardCrawler
{
    internal class Program
    {
        private static readonly List<string> BasicLands = ["Forest", "Island", "Mountain", "Plains", "Swamp"];
        private static readonly List<string> ExcludedCards = [];

        private static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            ConsoleUi.ShowBanner();

            CrawlerOptions? options = ArgumentParser.Parse(args);
            if (options == null)
            {
                return;
            }

            if (options.UpdateScryfallPriceCache && !string.IsNullOrWhiteSpace(options.UpdateScryfallFile))
            {
                Console.WriteLine("Updating Scryfall cache...");
                await Api.UpdateLocalCardData(options.UpdateScryfallFile);
                Console.WriteLine("Done.");
                return;
            }

            Reader.ReaderEventHandler += (s, e) =>
            {
                Debug.WriteLine(e.Message);
            };

            List<string> lines = [];
            if (options.InputPath != null)
            {
                lines = [.. File.ReadLines(options.InputPath).Where(l => !string.IsNullOrWhiteSpace(l))];
                if (lines.Count == 0)
                {
                    Console.WriteLine("No cards found in input file.");
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(options.ExcludeFile) && File.Exists(options.ExcludeFile))
            {
                foreach (string name in File.ReadLines(options.ExcludeFile).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    ExcludedCards.Add(Utilities.CleanCardName(name));
                }
            }

            Core.Interfaces.ICardDataProvider provider;
            if (options.DataSource == "cardmarket")
            {
                provider = new CardmarketProvider();
                Console.WriteLine("Using Cardmarket as data source.\r\n");
                await CheckCardSource(provider);
            }
            else
            {
                string cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scryfall_prices.json");
                if (!File.Exists(cachePath))
                {
                    cachePath = "scryfall_prices.json";
                }

                ScryfallProvider scryfall = new(cachePath);
                await scryfall.InitializeAsync();
                provider = scryfall;

                Console.WriteLine("Using Scryfall as data source.\r\n");
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

                string sym, info = "", price;
                if (card is not null)
                {
                    // Use clean name from card provider if available
                    statusList[i] = new(card.Name) { Count = count };

                    sym = include ? "✔️" : "~";
                    decimal rowTotal = card.PriceTrend * count;
                    price = $"{rowTotal:0.00}€";

                    if (count > 1)
                    {
                        statusList[i].Name += $" ({card.PriceTrend})";
                    }

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
                    sym = "✖ ";
                    price = "-.--€";
                    info = "not found";
                    statusList[i] = new StatusEntry(cleanName) { Count = count };
                }
                statusList[i].Symbol = sym;
                statusList[i].Price = price;
                statusList[i].Info = info;

                ConsoleUi.PrintStatusResult(statusList[i], original, resultLine);

                await Task.Delay(25);
            }

            Console.Clear();
            ConsoleUi.ShowBanner();
            ConsoleUi.DrawTable(statusList, options.BudgetLimit, total);

            if (!string.IsNullOrWhiteSpace(options.OutputPath))
            {
                await CsvExporter.SaveCsvAsync(options.OutputPath, statusList, total, options.BudgetLimit);
                Console.WriteLine($"\nSaved CSV to {options.OutputPath}");
            }
        }

        private static async Task CheckCardSource(Core.Interfaces.ICardDataProvider provider)
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
