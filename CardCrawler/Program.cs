using CardCrawler.Cardmarket;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardCrawler
{
    public class Program
    {
        private const string NotBold = "\u001b[0m";
        private const string Bold = "\x1b[1m";

        private const string Default = "\u001b[39m";
        private const string Red = "\x1b[91m";
        private const string Green = "\x1b[92m";

        public static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: CardCrawler <required input> [optional output]");
                return;
            }

            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            string input = args[0];
            string output = args.Length > 1 ? args[1] : "";

            var lines = File.ReadLines(input).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            if (lines.Count == 0)
            {
                Console.WriteLine("No cards found in input file");
                return;
            }

            var cards = new List<CardData>();
            var errorCards = new List<string>();

            Reader.ReaderEventHandler += Reader_ReaderEventHandler;

            decimal total = 0;
            int pos = 0;
            int max = lines.Count;

            Console.Clear();
            Console.Write("Checking cardmarket...");

            if (!await Reader.CheckConnection())
            {
                Console.WriteLine($"{Bold}{Red}Can't connect to Cardmarket!{Default}{NotBold}");
                return;
            }

            ClearCurrentConsoleLine();
            Console.WriteLine("Cardmarket is available.\r\n");

            foreach (var line in lines)
            {
                pos++;
                Console.Write($"{line}...");
                var card = await Reader.GetCardData(line);
                ClearCurrentConsoleLine();
                if (card != null)
                {
                    cards.Add(card);
                    total += card.PriceTrend;
                    string name = card.Name.Length < 40 ? $"{card.Name,-40}" : $"{card.Name[..37]}...";
                    string totals = $"{(total <= 20.00M ? Green : Red)}{total,5}€{Default}";
                    Console.WriteLine($"{pos:000} / {max:000} > {name}: {card.PriceTrend}€{"",-5}Total: {totals}");
                }
                else
                {
                    errorCards.Add(line);
                    Console.WriteLine($"\u001b[91mERROR: Could not find '{line}'!\u001b[39m");
                }
            }

            Console.WriteLine($"\r\n{(cards.Sum(c => c.PriceTrend) <= 20.00M ? Green : Red)}{Bold} => TOTAL: {cards.Sum(c => c.PriceTrend),5}€{Default}{NotBold}");

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine($"Writing card data to: {output}");
                var stringBuilder = new StringBuilder();
                total = 0;
                foreach (var card in cards)
                {
                    total += card.PriceTrend;
                    stringBuilder.AppendLine($"{card.Name}; {card.PriceTrend}€");
                }
                if (errorCards.Count != 0)
                {
                    stringBuilder.AppendLine($"\r\nERRORS;");
                    foreach (var item in errorCards)
                    {
                        stringBuilder.AppendLine($"{item}; 0,00€");
                    }
                }

                stringBuilder.AppendLine($"\r\nTOTAL; {cards.Sum(c => c.PriceTrend)}€");

                await File.WriteAllTextAsync(output, stringBuilder.ToString());
            }

            Console.ReadKey();
        }

        private static void Reader_ReaderEventHandler(object? sender, ReaderEventArgs e)
        {
            Console.Write(e.Message + (e.Done ? "\r\n" : ""));
        }

        static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, currentLineCursor);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}