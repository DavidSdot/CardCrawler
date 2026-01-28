using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CardCrawler.Cardmarket
{
    public static partial class Utilities
    {

        [GeneratedRegex(@"^(?:\d*[x|X]*\s*)(?<name>.*?)(?:\s\(.*)?$", RegexOptions.Singleline)]
        private static partial Regex CardNameExtractionRegex();
        [GeneratedRegex(@"\w+")]
        private static partial Regex WordExtractionRegex();
        [GeneratedRegex(@"\s")]
        private static partial Regex WhitespaceRegex();

        public static string CleanCardName(string cardName)
        {
            cardName = cardName.Replace("\uFEFF", "").Trim();
            cardName = CardNameExtractionRegex().Match(cardName).Groups["name"].Value;
            return cardName;
        }

        public static string UrlEncodeCardName(string cardName)
        {
            cardName = string.Join(" ", WordExtractionRegex().Matches(cardName).Select(m => m.Value));
            cardName = WhitespaceRegex().Replace(cardName, "-");
            return cardName;
        }

    }
}