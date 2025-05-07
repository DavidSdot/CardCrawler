using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CardCrawler.Cardmarket
{
    public static class Utilities
    {
        private static readonly Regex CleanCardNameRegex = new(@"^\d+\s+([^\(]+)", RegexOptions.Compiled);
        private static readonly Regex RemovePrefixesRegex = new(@"^\d+\s*[xX]?\s*", RegexOptions.Compiled);
        private static readonly Regex RemoveParenthesesAndSuffixRegex = new(@"\s*\(.*?\)\s*\d*$", RegexOptions.Compiled);
        private static readonly Regex RemoveParenthesesRegex = new(@"\s*\(.*?\)", RegexOptions.Compiled);
        private static readonly Regex RemoveAsteriskSuffixRegex = new(@"\s*\*.*", RegexOptions.Compiled);
        private static readonly Regex RemoveSlashesRegex = new(@"\s*\/", RegexOptions.Compiled);
        private static readonly Regex UrlEncodeRegex = new(@"[^a-zA-Z0-9\-]", RegexOptions.Compiled);

        public static string CleanCardName(string cardName)
        {
            if (string.IsNullOrWhiteSpace(cardName))
            {
                return string.Empty;
            }

            // Match and clean the card name
            Match match = CleanCardNameRegex.Match(cardName);
            if (match.Success)
            {
                string cleanedCardName = match.Groups[1].Value.Trim()
                    .Replace("//", " ")
                    .Replace(" / ", " ")
                    .Replace("/", " ");
                return cleanedCardName;
            }

            // Apply regex replacements for other cases
            string cleanedEntry = RemovePrefixesRegex.Replace(cardName, "");
            cleanedEntry = RemoveParenthesesAndSuffixRegex.Replace(cleanedEntry, "");
            cleanedEntry = RemoveParenthesesRegex.Replace(cleanedEntry, "");
            cleanedEntry = RemoveAsteriskSuffixRegex.Replace(cleanedEntry, "");
            cleanedEntry = RemoveSlashesRegex.Replace(cleanedEntry, "");
            return cleanedEntry.Trim();
        }

        public static string UrlEncodeCardName(string cardName)
        {
            if (string.IsNullOrWhiteSpace(cardName))
            {
                return string.Empty;
            }

            string formattedName = cardName.Trim();
            formattedName = Regex.Replace(formattedName, @"^\d+\s*", ""); // Remove leading numbers
            formattedName = Regex.Replace(formattedName, @"-", "");   // Remove hyphens
            formattedName = Regex.Replace(formattedName, @"\s+", "-");   // Replace spaces with hyphens
            formattedName = Regex.Replace(formattedName, @"'+", "");    // Remove apostrophes
            formattedName = formattedName.Replace("\\P{L}+", "");  // Remove invalid characters
            return formattedName;
        }

        public static string GetEdgePath()
        {
            const string regKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe";
            string path = Registry.GetValue(regKey, "", null) as string;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                return path;
            }

            string[] candidates =  [      
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                         "Microsoft\\Edge\\Application\\msedge.exe"),                       
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                "Microsoft\\Edge\\Application\\msedge.exe")    
                ];
            foreach (string p in candidates)
            {
                if (File.Exists(p))
                {
                    return p;
                }
            }

            return "msedge.exe";
        }

    }
}