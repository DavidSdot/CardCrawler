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
        public static async Task SaveCsvAsync(string outputPath, List<StatusEntry> list, decimal total, decimal budgetLimit)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }
            StringBuilder sb = new();
            _ = sb.AppendLine("Symbol;Count;Price;Info;Name");
            foreach (StatusEntry e in list)
            {
                _ = sb.AppendLine($"{e.Symbol};{e.Count};{e.Price};{e.Info};{e.Name.Replace("\"", "\"\"")}");
            }

            int totalCards = list.Sum(e => e.Count);
            if (budgetLimit > 0)
            {
                _ = sb.AppendLine($"TOTAL;{totalCards};{total:0.00}€;{(total > budgetLimit ? "FAILED" : "PASSED")};");
            }
            else
            {
                _ = sb.AppendLine($"TOTAL;{totalCards};{total:0.00}€;OK;");
            }
            await File.WriteAllTextAsync(outputPath, sb.ToString());
        }
    }
}
