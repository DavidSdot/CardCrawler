using CardCrawler.Browser;
using HtmlAgilityPack;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CardCrawler.Cardmarket
{

    public class ReaderEventArgs(string msg) : EventArgs
    {
        public string Message { get; } = msg;
    }

    public static class Reader
    {

        public const string BaseUrl = "https://www.cardmarket.com";
        public const string CardUrl = BaseUrl + "/en/Magic/Cards/";
        public const string SearchUrl = BaseUrl + "/en/Magic/AdvancedSearch?doSubmit=1&cardName=";

        private static readonly string[] Parameters =
        [
            "sellerCountry=1,2,3,33,35,5,6,8,9,11,12,7,14,15,37,16,17,21,18,19,20,22,23,24,25,26,27,29,31,30,10,28",
            "minCondition=4",
            "language=1,3"
        ];

        public static event EventHandler<ReaderEventArgs>? ReaderEventHandler;

        public static async Task<bool> CheckConnection()
        {
            try
            {
                string? html = await Edge.GetPageContent("https://www.cardmarket.com");
                return !string.IsNullOrWhiteSpace(html);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<CardData?> GetCardData(string cardName)
        {
            CardData card = new(cardName);
            string urlName = Utilities.UrlEncodeCardName(card.Name);

            Log($"Search by URL...{urlName}");
            string url = $"{CardUrl}{urlName}?{string.Join("&", Parameters)}";
            string? html = await Edge.GetPageContent(url);

            if (!string.IsNullOrWhiteSpace(html) && ParseCardPage(html) is CardData foundCard)
            {
                foundCard.UrlName = urlName;
                return foundCard;
            }

            Log($"Search by Name...{card.Name}");
            if (await SearchByCardNameAsync(cardName) is CardData namedCard)
            {
                Log($"Changed to: {namedCard.UrlName}...");
                url = $"{CardUrl}{namedCard.UrlName}?{string.Join("&", Parameters)}";
                html = await Edge.GetPageContent(url);
                if (html != null)
                {
                    return ParseCardPage(html);
                }
            }

            return null;
        }

        private static async Task<CardData?> SearchByCardNameAsync(string cardName)
        {
            string name = HttpUtility.UrlEncode(cardName);
            string url = $"{SearchUrl}{name}";
            string? html = await Edge.GetPageContent(url);

            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            HtmlDocument htmlDoc = new();
            htmlDoc.LoadHtml(html);

            HtmlNode? resultNode = htmlDoc.DocumentNode.SelectSingleNode("/html/body/main/section/div[2]/div[1]/div[2]/div[1]/div[1]/h2/a");
            if (resultNode != null)
            {
                string? urlName = resultNode.Attributes["href"]?.Value.Split('/').Last();
                if(string.IsNullOrWhiteSpace(urlName))
                {
                    return null;
                }
                return new CardData(cardName) { UrlName = urlName };
            }

            return null;
        }

        private static CardData? ParseCardPage(string html)
        {
            HtmlDocument doc = new();
            doc.LoadHtml(html);

            // Kartentitel
            HtmlNode? titleNode = doc.DocumentNode.SelectSingleNode("/html/body/main/div[2]/div/h1/text()");
            if (titleNode == null)
            {
                return null;
            }

            string title = titleNode.InnerText.Trim();
            if (title.Contains("404"))
            {
                return null;
            }

            Log($"Card: {title}");

            CardData card = new(title);

            HtmlNode? trendNode = doc.DocumentNode.SelectSingleNode("//*[@id=\"info\"]/div/dl/dd[4]/span");
            if (trendNode != null &&
                decimal.TryParse(trendNode.InnerText.Replace("€", "").Trim().Replace(",", "."),
                                 NumberStyles.Any,
                                 CultureInfo.InvariantCulture,
                                 out decimal trend))
            {
                card.PriceTrend = trend;
            }

            HtmlNodeCollection? priceNodes = doc.DocumentNode.SelectNodes(
                "//div[contains(@class,'price-container')]//span[contains(@class,'bold')]");
            if (priceNodes != null)
            {
                card.Prices.Clear();
                foreach (HtmlNode n in priceNodes)
                {
                    string text = n.InnerText.Replace("€", "").Trim();
                    if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal p))
                    {
                        card.Prices.Add(p);
                    }
                }
            }

            return card;
        }

        private static void Log(string msg)
        {
            ReaderEventHandler?.Invoke(null, new ReaderEventArgs(msg));
        }
    }
}
