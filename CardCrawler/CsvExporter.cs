using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace CardCrawler
{
    public static class CsvExporter
    {
        public static async Task SaveCsvAsync(CrawlerOptions options, List<StatusEntry> list, decimal total)
        {
            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                return;
            }
            StringBuilder sb = new();
            _ = sb.AppendLine("Symbol;Count;Price;Info;Name");
            foreach (StatusEntry e in list)
            {
                _ = sb.AppendLine($"{e.Symbol};{e.Count};{e.UnitPrice:0.00}€;{e.Info};{e.Name.Replace("\"", "\"\"")}");
            }

            int totalCards = list.Sum(e => e.Count);
            bool passedBudget = options.BudgetLimit <= 0 || total <= options.BudgetLimit;

            if (passedBudget || options.PriceLimit > 0)
            {
                passedBudget = !list.Any(e => e.UnitPrice > options.PriceLimit);
            }

            if (options.BudgetLimit > 0 || options.PriceLimit > 0)
            {
                _ = sb.AppendLine($"TOTAL;{totalCards};{total:0.00}€;{(passedBudget ? "PASSED" : "FAILED")};");
            }
            else
            {
                _ = sb.AppendLine($"TOTAL;{totalCards};{total:0.00}€;;");
            }
            await File.WriteAllTextAsync(options.OutputPath, sb.ToString());
        }
    }
}
