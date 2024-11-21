using System.Text.RegularExpressions;

namespace CardCrawler.Cardmarket
{
    public class Utilities
    {

        public static string CleanCardName(string cardName)
        {
            // Suche nach dem ersten Leerzeichen und extrahiere alles bis zur ersten öffnenden Klammer '(' falls vorhanden
            var match = Regex.Match(cardName, @"^\d+\s+([^\(]+)");

            // Wenn ein Treffer gefunden wurde, ersetze "//" durch ein Leerzeichen
            if (match.Success)
            {
                string cleanedCardName = match.Groups[1].Value.Trim();

                // Ersetze "//" durch ein Leerzeichen
                cleanedCardName = cleanedCardName.Replace("//", " ");
                cleanedCardName = cleanedCardName.Replace(" / ", " ");
                cleanedCardName = cleanedCardName.Replace("/", " ");

                return cleanedCardName;
            }

            // Entferne '1x' oder ähnliche Präfixe sowie alles in Klammern und nach *
            string ElsecleanedEntry = Regex.Replace(cardName, @"^\d+\s*[xX]?\s*", ""); // Entfernt führende Anzahl
            ElsecleanedEntry = Regex.Replace(ElsecleanedEntry, @"\s*\(.*?\)\s\d+$", ""); // Entfernt Klammern und alles danach vom Ende aus
            ElsecleanedEntry = Regex.Replace(ElsecleanedEntry, @"\s*\(.*?\)", ""); // Entfernt Klammern und ihren Inhalt
            ElsecleanedEntry = Regex.Replace(ElsecleanedEntry, @"\s*\*.*", ""); // Entfernt * und alles danach
            ElsecleanedEntry = Regex.Replace(ElsecleanedEntry, @"\s*\/", ""); // Entfernt /
            return ElsecleanedEntry.Trim();
        }

        public static string UrlEncodeCardName(string cardName)
        {
            string formattedName = cardName.Trim();
            formattedName = Regex.Replace(formattedName, @"^\d+\s*", "");
            formattedName = Regex.Replace(formattedName, @"\s+", "-");
            formattedName = Regex.Replace(formattedName, @"'+", "");
            formattedName = Regex.Replace(formattedName, @"[^a-zA-Z0-9\-]", "");

            return formattedName;
        }

    }
}
