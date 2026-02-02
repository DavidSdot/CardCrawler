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

        public static void DrawTable(List<StatusEntry> list, decimal budget, decimal total)
        {
            int nameW = list.Max(l => l.Name.Length);
            int countW = 5; // Fixed width for count
            int priceW = list.Max(l => l.Price.ToString().Length);
            int infoW = 12;

            Console.ResetColor();
            Console.WriteLine($" ST | {"Count".PadLeft(countW)} | {"Name".PadRight(nameW)} | {"Price".PadLeft(priceW)} | {"Info".PadRight(infoW)}");
            Console.WriteLine(new string('-', 3 + 2 + countW + 3 + nameW + 3 + priceW + 3 + infoW));

            foreach (StatusEntry e in list)
            {

                Console.ForegroundColor = ConsoleColor.Green;
                if (e.ExceedsLimit || e.Symbol != "✔")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.Write($" {e.Symbol} ");
                Console.ResetColor();
                Console.Write(" | ");
                Console.Write($"{e.Count}x".PadLeft(countW));
                Console.Write(" | ");
                Console.Write(e.Name.PadRight(nameW));
                Console.Write(" | ");
                Console.Write(e.Price.PadLeft(priceW));
                Console.Write(" | ");
                Console.Write(e.Info.PadRight(infoW));

                Console.WriteLine();
            }
            Console.WriteLine(new string('-', 3 + 2 + countW + 3 + nameW + 3 + priceW + 3 + infoW));

            int totalCards = list.Sum(e => e.Count);
            string status = (budget > 0 && total > budget) || list.Any(e => e.ExceedsLimit) ? "NOT OK" : "OK";

            // Format: Total: xx.xx€ | Cards: XX | Status: OK | Remaining: xx.xx€
            Console.WriteLine($"{"     ".PadRight(countW + 3 + nameW)}  Total: {total:0.00}€ | Cards: {totalCards} | Status: {status}");
            if (budget > 0)
            {
                Console.WriteLine($"{"     ".PadRight(countW + 3 + nameW)}  Budget Left: {budget - total:0.00}€");
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

        public static void PrintStatusResult(StatusEntry entry, string originalName, int resultLine)
        {
            // Use entry data or reconstruct string? The original code formatted a string:
            // $"{sym} {info} {price}: {(card is null ? original : card.Name)}"
            // statusEntry contains sym, price, info. Name is stored.
            // But originalName might differ from entry.Name if card found under different name.
            // Let's print using the entry data.

            string displayName = entry.Name;
            // Logic in Program.cs was: card is null ? original : card.Name
            // If card was found, entry.Name = card.Name. If not found, statusList[i] was initialized with original name?
            // Actually in Program.cs: `statusList[i] = new(card.Name)` if found.
            // Otherwise initialized with `lines.Select(n => new StatusEntry(n))` -> original name. 
            // So entry.Name covers both cases mostly.

            string statusResult = $"{entry.Symbol} {entry.Info} {entry.Price}: {displayName}";
            Console.SetCursorPosition(0, resultLine);
            Console.Write(statusResult.PadRight(Console.WindowWidth));
        }
    }
}
