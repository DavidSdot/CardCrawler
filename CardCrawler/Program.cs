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
        private static bool excludeFirst;
        private static decimal budgetLimit = 20.00M;

        private static readonly List<string> BasicLands = new()
            { "Forest","Island","Mountain","Plains","Swamp" };
        private static readonly List<string> ExcludedCards = new();

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

            var unnamed = new List<string>();
            bool showHelp = false;

            foreach (string arg in args)
            {
                if (arg.StartsWith("--exclude=", StringComparison.Ordinal))
                {
                    excludeFile = arg.Substring("--exclude=".Length);
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
                else if (arg.Equals("--help", StringComparison.Ordinal) || arg.Equals("-h", StringComparison.Ordinal))
                {
                    showHelp = true;
                }
                else if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    Console.WriteLine($"Unknown option: {arg}");
                    return;
                }
                else
                {
                    unnamed.Add(arg);
                }
            }

            if (unnamed.Count == 0 || showHelp)
            {
                PrintUsage();
                return;
            }

            inputPath = unnamed[0];
            outputPath = unnamed.Count > 1 ? unnamed[1] : null;

            List<string> lines = [.. File.ReadLines(inputPath!).Where(l => !string.IsNullOrWhiteSpace(l))];
            if (lines.Count == 0)
            {
                Console.WriteLine("No cards found in input file.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(excludeFile) && File.Exists(excludeFile))
            {
                foreach (string name in File.ReadLines(excludeFile).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    ExcludedCards.Add(Utilities.CleanCardName(name));
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
            Console.WriteLine("OK\r\n");
            if (excludeFirst)
            {
                Console.WriteLine("Excluding first card from total");
            }
            if (excludeBasics)
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
                string cleanName = Utilities.CleanCardName(original);

                const int barW = 20;
                int filled = (int)((i + 1) * barW / (double)lines.Count);
                string bar = "[" + new string('█', filled) + new string('░', barW - filled) + "]";
                int pct = (i + 1) * 100 / lines.Count;

                Console.SetCursorPosition(0, headerLine);
                Console.Write($"> Processing {i + 1}/{lines.Count} {bar} ({pct}%) | Total: {total:0.00}€ | Left: {budgetLimit - total:0.00}€".PadRight(Console.WindowWidth));

                Console.SetCursorPosition(0, statusLine);
                Console.Write($"↪ Fetching: {cleanName}".PadRight(Console.WindowWidth));

                CardData? card = await reader.GetCardData(cleanName);

                bool isBasic = BasicLands.Contains(cleanName);
                bool isExcluded = ExcludedCards.Contains(cleanName);
                bool include = !((excludeBasics && isBasic) || isExcluded);

                if (i == 0 && excludeFirst)
                {
                    include = false;
                }

                string sym, info, price;
                if (card is not null)
                {
                    statusList[i] = new(card.Name);
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
                    info = "included";
                    info = "notfound";
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
            int nameW = list.Max(l => l.Name.Length);
            int priceW = list.Max(l => l.Price.ToString().Length);
            int infoW = 12;
            Console.WriteLine($" ST | {"Name".PadRight(nameW)} | {"Price".PadLeft(priceW)} | {"Info".PadRight(infoW)}");
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
            Console.WriteLine($"{"     ".PadRight(nameW)}  Total: {total:0.00}€ | Budget: {budget - total:0.00}€");
        }

        private static async Task SaveCsvAsync(List<StatusEntry> list, decimal total)
        {
            StringBuilder sb = new();
            _ = sb.AppendLine("Symbol;Price;Info;Name");
            foreach (StatusEntry e in list)
            {
                _ = sb.AppendLine($"{e.Symbol};{e.Price};{e.Info};{e.Name.Replace("\"", "\"\"")}");
            }

            _ = sb.AppendLine($"TOTAL;{total:0.00}€;{(total > budgetLimit ? "FAILED" : "PASSED")};");
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
            Console.WriteLine("  --excludeFirst         Exclude the first card from from total (eg. your commander).");
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
