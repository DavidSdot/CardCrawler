using System.Linq;
using System.Text.RegularExpressions;

namespace CardCrawler.Cardmarket
{
    public static partial class Utilities
    {

        [GeneratedRegex(@"^(?:(?<count>\d+)[x|X]*\s+)?(?<name>.*?)(?:\s\(.*)?$", RegexOptions.Singleline)]
        private static partial Regex CardLineParsingRegex();

        [GeneratedRegex(@"\w+")]
        private static partial Regex WordExtractionRegex();
        [GeneratedRegex(@"\s")]
        private static partial Regex WhitespaceRegex();

        public static string CleanCardName(string cardName)
        {
            (int _, string? name) = ParseCardLine(cardName);
            return name;
        }

        public static (int Count, string Name) ParseCardLine(string line)
        {
            line = line.Replace("\uFEFF", "").Trim();
            if (string.IsNullOrWhiteSpace(line)) return (1, string.Empty);

            int count = 1;
            Match countMatch = Regex.Match(line, @"^(?:(?<count>\d+)[xX]?\s+)");
            if (countMatch.Success && int.TryParse(countMatch.Groups["count"].Value, out int c))
            {
                count = c;
                line = line[countMatch.Length..].Trim();
            }

            string cleanName = line;
            cleanName = Regex.Replace(cleanName, @"\s+\*[A-Za-z0-9_*]+\*\s*$", "", RegexOptions.IgnoreCase).Trim();
            cleanName = Regex.Replace(cleanName, @"\s+[\(\[][A-Za-z0-9_\-\+]+[\)\]]\s+[A-Za-z0-9\-\/]+\s*$", "", RegexOptions.IgnoreCase).Trim();
            cleanName = Regex.Replace(cleanName, @"\s+[\(\[][A-Za-z0-9_\-\+]+[\)\]]\s*$", "", RegexOptions.IgnoreCase).Trim();
            cleanName = Regex.Replace(cleanName, @"\s+#[0-9A-Za-z\-]+\s*$", "", RegexOptions.IgnoreCase).Trim();

            return (count, cleanName);
        }

        public static string UrlEncodeCardName(string cardName)
        {
            cardName = string.Join(" ", WordExtractionRegex().Matches(cardName).Select(m => m.Value));
            cardName = WhitespaceRegex().Replace(cardName, "-");
            return cardName;
        }

    }
}