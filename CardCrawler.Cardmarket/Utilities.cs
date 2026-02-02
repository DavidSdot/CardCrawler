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
            Match match = CardLineParsingRegex().Match(line);

            string name = match.Groups["name"].Value;
            string countStr = match.Groups["count"].Value;

            int count = 1;
            if (!string.IsNullOrEmpty(countStr) && int.TryParse(countStr, out int c))
            {
                count = c;
            }

            return (count, name);
        }

        public static string UrlEncodeCardName(string cardName)
        {
            cardName = string.Join(" ", WordExtractionRegex().Matches(cardName).Select(m => m.Value));
            cardName = WhitespaceRegex().Replace(cardName, "-");
            return cardName;
        }

    }
}