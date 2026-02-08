using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CardCrawler
{
    public static class ConsoleUi
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

        public static void ShowBanner()
        {
            Console.ResetColor();
            Console.Clear();
            foreach (string line in Banner)
            {
                Console.WriteLine(line);
                Thread.Sleep(40);
            }
            Console.WriteLine();
        }

        public static void DrawTable(List<StatusEntry> list, CrawlerOptions options, decimal total)
        {
            // Dynamic column visibility
            bool showInfo = options.ExcludeBasics || options.ExcludeFirst || !string.IsNullOrEmpty(options.ExcludeFile);

            // Calculate column widths
            int countW = 7;
            int nameW = Math.Max(20, list.Max(l => l.Name.Length) + 2);
            int unitPriceW = 12;
            int totalPriceW = 12;
            int infoW = showInfo ? Math.Max(10, list.Max(l => l.Info.Length) + 2) : 0;

            // Box drawing characters
            string tl = "┌";
            string tr = "┐";
            string bl = "└";
            string br = "┘";
            char h = '─';
            string v = "│";
            string t = "┬";
            string b = "┴";
            string x = "┼";

            // Build header
            Console.ResetColor();

            // Top border
            Console.Write(tl + new string(h, 4) + t + new string(h, countW) + t + new string(h, nameW) + t + new string(h, unitPriceW) + t + new string(h, totalPriceW));
            if (showInfo)
                Console.Write(t + new string(h, infoW));
            Console.WriteLine(tr);

            // Header Row
            Console.Write(v + " ST " + v + " Count ".PadRight(countW) + v + " Name".PadRight(nameW) + v + " Unit Price ".PadLeft(unitPriceW) + v + " Total Price".PadLeft(totalPriceW));
            if (showInfo)
                Console.Write(v + " Info".PadRight(infoW));
            Console.WriteLine(v);

            // Separator
            Console.Write(x.PadLeft(1, '├') + new string(h, 4) + x + new string(h, countW) + x + new string(h, nameW) + x + new string(h, unitPriceW) + x + new string(h, totalPriceW));
            if (showInfo)
                Console.Write(x + new string(h, infoW));
            Console.WriteLine("┤");

            foreach (StatusEntry e in list)
            {
                Console.ResetColor();
                Console.Write(v);
                Console.Write($" {e.Symbol}  ");

                Console.Write(v);
                Console.Write($"{e.Count}x".PadLeft(countW - 1) + " ");
                Console.Write(v + " ");
                Console.Write(e.Name.PadRight(nameW - 1));
                Console.Write(v + " ");
                if (e.ExceedsLimit)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.Write($"{e.UnitPrice:0.00}€".PadLeft(unitPriceW - 1));
                Console.ResetColor();
                Console.Write(v + " ");
                Console.Write($"{e.TotalPrice:0.00}€".PadLeft(totalPriceW - 1));

                if (showInfo)
                {
                    Console.Write(v + " ");
                    Console.Write(e.Info.PadRight(infoW - 1));
                }

                Console.WriteLine(v);
            }

            // Bottom border
            Console.Write(bl + new string(h, 4) + b + new string(h, countW) + b + new string(h, nameW) + b + new string(h, unitPriceW) + b + new string(h, totalPriceW));
            if (showInfo)
                Console.Write(b + new string(h, infoW));
            Console.WriteLine(br);

            int totalCards = list.Sum(e => e.Count);
            string status = (options.BudgetLimit > 0 && total > options.BudgetLimit) || list.Any(e => e.ExceedsLimit) ? "NOT OK" : "OK";

            Console.WriteLine();
            Console.WriteLine($" Total: {total:0.00}€");
            Console.WriteLine($" Cards: {totalCards}");
            Console.WriteLine($" Status: {status}");

            if (options.BudgetLimit > 0)
            {
                decimal left = options.BudgetLimit - total;
                Console.Write(" Budget Left: ");
                if (left < 0)
                    Console.ForegroundColor = ConsoleColor.Red;
                else
                    Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{left:0.00}€");
                Console.ResetColor();
            }
        }

        public static void UpdateProgress(int currentIndex, int totalCount, decimal currentTotal, decimal budgetLimit, int headerLine, string cleanName, int statusLine)
        {
            const int barW = 20;
            int filled = (int)((currentIndex + 1) * barW / (double)totalCount);
            string bar = "[" + new string('█', filled) + new string('░', barW - filled) + "]";
            int pct = (currentIndex + 1) * 100 / totalCount;

            Console.SetCursorPosition(0, headerLine);
            string budgetLeft = "";
            if (budgetLimit > 0)
            {
                budgetLeft = $"| Left: {budgetLimit - currentTotal:0.00}€";
            }
            Console.Write($"> Processing {currentIndex + 1}/{totalCount} {bar} ({pct}%) | Total: {currentTotal:0.00}€{budgetLeft}".PadRight(Console.WindowWidth));

            Console.SetCursorPosition(0, statusLine);
            Console.Write($"↪ Fetching: {cleanName}".PadRight(Console.WindowWidth));
        }

        public static void PrintStatusResult(StatusEntry entry, int resultLine)
        {
            string displayName = entry.Name;
            string priceStr = entry.TotalPrice > 0 ? $"{entry.TotalPrice:0.00}€" : "-.--€";
            string statusResult = $"{entry.Symbol} {entry.Info} {priceStr}: {displayName}";
            Console.SetCursorPosition(0, resultLine);
            Console.Write(statusResult.PadRight(Console.WindowWidth));
        }
    }
}
