using CardCrawler.Cardmarket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardCrawler
{
    internal class Program
    {

        private static readonly string[] Banner =
        [
            "▄████▄   ▄▄▄       ██▀███  ▓█████▄  ▄████▄   ██▀███   ▄▄▄       █     █░ ██▓    ▓█████  ██▀███  ",
            "▒██▀ ▀█  ▒████▄    ▓██ ▒ ██▒▒██▀ ██▌▒██▀ ▀█  ▓██ ▒ ██▒▒████▄    ▓█░ █ ░█░▓██▒    ▓█   ▀ ▓██ ▒ ██▒",
            "▒▓█    ▄ ▒██  ▀█▄  ▓██ ░▄█ ▒░██   █▌▒▓█    ▄ ▓██ ░▄█ ▒▒██  ▀█▄  ▒█░ █ ░█ ▒██░    ▒███   ▓██ ░▄█ ▒",
            "▒▓▓▄ ▄██▒░██▄▄▄▄██ ▒██▀▀█▄  ░▓█▄   ▌▒▓▓▄ ▄██▒▒██▀▀█▄  ░██▄▄▄▄██ ░█░ █ ░█ ▒██░    ▒▓█  ▄ ▒██▀▀█▄  ",
            "▒ ▓███▀ ░ ▓█   ▓██▒░██▓ ▒██▒░▒████▓ ▒ ▓███▀ ░░██▓ ▒██▒ ▓█   ▓██▒░░██▒██▓ ░██████▒░▒████▒░██▓ ▒██▒",
            "░ ░▒ ▒  ░ ▒▒   ▓▒█░░ ▒▓ ░▒▓░ ▒▒▓  ▒ ░ ░▒ ▒  ░░ ▒▓ ░▒▓░ ▒▒   ▓▒█░░ ▓░▒ ▒  ░ ▒░▓  ░░░ ▒░ ░░ ▒▓ ░▒▓░",
            "  ░  ▒     ▒   ▒▒ ░  ░▒ ░ ▒░ ░ ▒  ▒   ░  ▒     ░▒ ░ ▒░  ▒   ▒▒ ░  ▒ ░ ░  ░ ░ ▒  ░ ░ ░  ░  ░▒ ░ ▒░",
            "░          ░   ▒     ░░   ░  ░ ░  ░ ░          ░░   ░   ░   ▒     ░   ░    ░ ░      ░     ░░   ░ ",
            "░ ░            ░  ░   ░        ░    ░ ░         ░           ░  ░    ░        ░  ░   ░  ░   ░     ",
            "░                            ░      ░                                                            "
        ];

        private static string? inputPath;
        private static string? outputPath;
        private static string? excludeFile;
        private static bool excludeBasics;
        private static decimal budgetLimit = 20.00M;

        private static readonly HashSet<string> BasicLands = new(StringComparer.InvariantCultureIgnoreCase)
            { "Forest","Island","Mountain","Plains","Swamp" };
        private static readonly HashSet<string> ExcludedCards = new(StringComparer.InvariantCultureIgnoreCase);

        private class StatusEntry(string name)
        {
            public string Name { get; } = name;
            public string Symbol { get; set; } = " ";
            public string Price { get; set; } = "--";
            public string Info { get; set; } = "waiting";
        }

        private static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.Clear();

            foreach (string line in Banner)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine();

            List<string> unnamed = [];
            bool showHelp = false;
            foreach (string arg in args)
            {
                if (arg.StartsWith("--exclude="))
                {
                    excludeFile = arg["--exclude=".Length..];
                }
                else if (arg == "--no-basics")
                {
                    excludeBasics = true;
                }
                else if (arg.StartsWith("--budget=") &&
                         decimal.TryParse(arg["--budget=".Length..], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal b))
                {
                    budgetLimit = b;
                }
                else if (arg == "--help" || arg == "-h")
                {
                    showHelp = true;
                }
                else if (arg.StartsWith("--"))
                {
                    Console.WriteLine($"Unknown option: {arg}");
                    return;
                }
                else
                {
                    unnamed.Add(arg);
                }
            }

            if (unnamed.Count < 1 || showHelp)
            {
                PrintUsage();
                return;
            }
            inputPath = unnamed[0];
            if (unnamed.Count > 1)
            {
                outputPath = unnamed[1];
            }

            List<string> lines = [.. File.ReadLines(inputPath!).Where(l => !string.IsNullOrWhiteSpace(l))];
            if (lines.Count == 0)
            {
                Console.WriteLine("No cards found in input file.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(excludeFile) && File.Exists(excludeFile))
            {
                foreach (string name in File.ReadLines(excludeFile!))
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        _ = ExcludedCards.Add(Utilities.CleanCardName(name.Trim()));
                    }
                }
            }

            Reader reader = new();

            reader.ReaderEventHandler += (s, e) =>
            {
                Debug.WriteLine(e.Message);
            }
            ;

            Console.Write("Checking Cardmarket... ");
            while (!await Reader.CheckConnection())
            {
                Console.WriteLine("FAILED, retrying...");
                await Task.Delay(2000);
                Console.Write("Checking Cardmarket... ");
            }
            Console.WriteLine("OK\n");

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
                string cleanName = Utilities.CleanCardName(original);

                const int barW = 20;
                int filled = (int)((i + 1) * barW / (double)lines.Count);
                string bar = "[" + new string('█', filled) + new string('░', barW - filled) + "]";
                int pct = (i + 1) * 100 / lines.Count;

                Console.SetCursorPosition(0, headerLine);
                Console.Write($"> Processing {i + 1}/{lines.Count} {bar} ({pct}%) | Total: {total:0.00}€ | Left: {budgetLimit - total:0.00}€".PadRight(Console.WindowWidth));

                Console.SetCursorPosition(0, statusLine);
                Console.Write($"↪ Fetching: {original}".PadRight(Console.WindowWidth));

                CardData? card = await reader.GetCardData(original);

                bool isBasic = BasicLands.Contains(cleanName);
                bool isExcluded = ExcludedCards.Contains(cleanName);
                bool include = !((excludeBasics && isBasic) || isExcluded);

                string sym, info, price;
                if (card is not null)
                {
                    sym = include ? "✓" : "~";
                    price = $"{card.PriceTrend:0.00}€";
                    info = include ? "included" : "excluded";
                    if (include)
                    {
                        total += card.PriceTrend;
                    }
                }
                else
                {
                    sym = "X";
                    price = "-.--€";
                    info = "Not found";
                }
                statusList[i].Symbol = sym;
                statusList[i].Price = price;
                statusList[i].Info = info;

                string statusResult =
                    $"{sym} {info} {price}: {(card is null ? original : card.Name)}";
                Console.SetCursorPosition(0, resultLine);
                Console.Write(statusResult.PadRight(Console.WindowWidth));

                await Task.Delay(50);
            }

            Console.WriteLine();
            DrawTable(statusList, budgetLimit, total);

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                await SaveCsvAsync(statusList, total);
                Console.WriteLine($"\nSaved CSV to {outputPath}");
            }
        }

        private static void DrawTable(List<StatusEntry> list, decimal budget, decimal total)
        {
            const int nameW = 40, priceW = 7, infoW = 12;
            Console.WriteLine($" ST | {"Name",-nameW} | {"Price",priceW} | {"Info",-infoW}");
            Console.WriteLine(new string('-', 3 + 2 + nameW + 3 + priceW + 3 + infoW));
            foreach (StatusEntry e in list)
            {
                Console.ForegroundColor = e.Symbol == "✓" ? ConsoleColor.Green
                                     : e.Symbol == "✖" ? ConsoleColor.Red
                                     : ConsoleColor.DarkGray;
                Console.Write($" {e.Symbol} ");
                Console.ResetColor();
                Console.Write(" | ");
                Console.Write(e.Name.PadRight(nameW));
                Console.Write(" | ");
                Console.Write(e.Price.PadLeft(priceW));
                Console.Write(" | ");
                Console.Write(e.Info.PadRight(infoW));
                Console.WriteLine();
            }
            Console.WriteLine(new string('-', 3 + 2 + nameW + 3 + priceW + 3 + infoW));
            Console.WriteLine($"{"",nameW + 2}  Total: {total:0.00}€ | Budget: {budget - total:0.00}€");
        }

        private static async Task SaveCsvAsync(List<StatusEntry> list, decimal total)
        {
            StringBuilder sb = new();
            _ = sb.AppendLine("Symbol;Name;Price;Info");
            foreach (StatusEntry e in list)
            {
                _ = sb.AppendLine($"{e.Symbol};\"{e.Name.Replace("\"", "\"\"")}\";{e.Price};{e.Info}");
            }

            _ = sb.AppendLine($"TOTAL;;{total:0.00}€;");
            await File.WriteAllTextAsync(outputPath!, sb.ToString());
        }

        private static void PrintUsage()
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
            Console.WriteLine("  --exclude=<file>       Path to a text file with card names to exclude");
            Console.WriteLine("                         (one name per line). These cards will be skipped");
            Console.WriteLine("                         in the total calculation.");
            Console.WriteLine("  --no-basics            Exclude basic lands (Forest, Island, etc.) from total.");
            Console.WriteLine("  --budget=<amount>      Set custom budget limit in EUR (default: 20.00).");
            Console.WriteLine("  -h, --help             Show this help message and exit.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  CardCrawler deck.txt");
            Console.WriteLine("  CardCrawler deck.txt prices.csv --budget=30.00");
            Console.WriteLine("  CardCrawler deck.txt prices.csv --exclude=ignore.txt --no-basics");
        }

    }
}
