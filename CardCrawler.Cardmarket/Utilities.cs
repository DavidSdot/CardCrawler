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

        [GeneratedRegex(@"^(?:\d*[x|X]*\s+)(?<name>.*?)(?:\s\(.*)?$", RegexOptions.Singleline)]
        private static partial Regex CardNameExtractionRegex();

        public static string CleanCardName(string cardName)
        {
           return CardNameExtractionRegex().Match(cardName).Groups["name"].Value;
        }

        public static string UrlEncodeCardName(string cardName)
        {
            cardName = string.Join(" ", Regex.Matches(cardName, @"\w+").Select(m => m.Value));
            cardName = Regex.Replace(cardName, @"\s", "-");
            return cardName;
        }

    }
}