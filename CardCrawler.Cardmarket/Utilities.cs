using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CardCrawler.Cardmarket
{
    public static partial class Utilities
    {
        private static readonly Regex CleanCardNameRegex = new(@"^\d+\s+([^\(]+)", RegexOptions.Compiled);
        private static readonly Regex RemovePrefixesRegex = new(@"^\d+\s*[xX]?\s*", RegexOptions.Compiled);
        private static readonly Regex RemoveParenthesesAndSuffixRegex = new(@"\s*\(.*?\)\s*\d*$", RegexOptions.Compiled);
        private static readonly Regex RemoveParenthesesRegex = new(@"\s*\(.*?\)", RegexOptions.Compiled);
        private static readonly Regex RemoveAsteriskSuffixRegex = new(@"\s*\*.*", RegexOptions.Compiled);
        private static readonly Regex RemoveSlashesRegex = new(@"\s*\/", RegexOptions.Compiled);
        private static readonly Regex UrlEncodeRegex = new(@"[^a-zA-Z0-9\-]", RegexOptions.Compiled);

        [GeneratedRegex(@"^(?:\d*[x|X]*\s+)(?<name>.*?)(?:\s\(.*)?$", RegexOptions.Singleline)]
        private static partial Regex CardNameExtractionRegex();

        public static string CleanCardName(string cardName)
        {
            return CardNameExtractionRegex().Match(cardName).Groups["name"].Value;
        }

        public static string UrlEncodeCardName(string cardName)
        {
            cardName = string.Join(" ", Regex.Matches(cardName, @"\w+").Select(m => m.Value));  // Remove invalid characters
            cardName = Regex.Replace(cardName, @"\s", "-");   // Remove hyphens
            return cardName;
        }

    }
}