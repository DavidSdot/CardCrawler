using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace CardCrawler.Cardmarket
{
    public static class Reader
    {

        static Reader()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7" },
                { "Accept-Language", "de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7" },
                { "Cache-Control", "max-age=0" },
                { "Connection", "keep-alive" },
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36" },
            };

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = false,
            };

            Client = new HttpClient(handler);
            Client.DefaultRequestHeaders.Referrer = new Uri(BaseUrl);
            foreach (var header in headers)
            {
                Client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public const string BaseUrl = @"https://www.cardmarket.com";
        public const string CardUrl = BaseUrl + @"/en/Magic/Cards/";
        public const string SearchUrl = BaseUrl + @"/en/Magic/AdvancedSearch?doSubmit=1&cardName=";

        private static readonly HttpClient Client;

        public static async Task<bool> CheckConnection()
        {
            HttpResponseMessage response = null;
            int tries = 0;
            do
            {
                tries++;
                try
                {
                    response = await Client.GetAsync(BaseUrl);
                }
                catch (Exception)
                {
                    continue;
                }

                if ((int)response.StatusCode == 429)
                {
                    ReaderEventHandler?.Invoke(null, new ReaderEventArgs($"Error: {response.StatusCode}, waiting 30s...", 30));
                    await Task.Delay(30000);
                }
                else if (response.StatusCode != HttpStatusCode.OK)
                {
                    ReaderEventHandler?.Invoke(null, new ReaderEventArgs($"Error: {response.StatusCode}, waiting 1s...", 1));
                    await Task.Delay(1000);
                }
            } while (tries < 5 && (response == null || response.StatusCode != HttpStatusCode.OK));

            return response != null && response.StatusCode == HttpStatusCode.OK;
        }

        public static event EventHandler<ReaderEventArgs> ReaderEventHandler;

        private static int Timeout = 30;
        private static int Delay = 2000;
        private static readonly string[] Paramters = new string[] {
            "sellerCountry=1,2,3,33,35,5,6,8,9,11,12,7,14,15,37,16,17,21,18,19,20,22,23,24,25,26,27,29,31,30,10,28", // EU
            "minCondition=4",  // good
            "language=1,3" // lang = de/eng
        };

        public static async Task<CardData> GetCardData(string cardname, bool retry = false)
        {
            var (Card, Response) = await CallDelayed(GetCardDataFromOffers(Client, cardname));
            if (Response.IsSuccessStatusCode)
            {
                return Card;
            }
            else if (retry)
            {
                Debug.WriteLine($"GetCardDataFromOffers: {Response.StatusCode}");
                return null;
            }
            Debug.WriteLine($"GetCardDataFromOffers: {Response.StatusCode}");

            if (Response.StatusCode == HttpStatusCode.NotFound)
            {
                LogEvent($"Search...");
                (Card, Response) = await CallDelayed(SearchByCardName(Client, cardname));
                if (Response.StatusCode == HttpStatusCode.OK && Card != null)
                {
                    LogEvent($"Changed to: {Card.UrlName}...");
                    (Card, Response) = await CallDelayed(GetCardDataFromOffers(Client, Card));
                    if (Response.IsSuccessStatusCode)
                    {
                        return Card;
                    }
                }
                else
                {
                    Debug.WriteLine($"SearchByCardName: {Response.StatusCode}");
                    Card = null;
                }
            }
            else if ((int)Response.StatusCode == 429)  // Too many requests
            {
                LogEvent($"Timeout for {Timeout}s");
                for (int i = 0; i < Timeout; i += 5)
                {
                    LogEvent($".");
                    await Task.Delay(5000);
                }
                Card = await CallDelayed(GetCardData(cardname, true));
                Timeout += 5;
                Delay += 250;
            }
            return Card;
        }

        private static async Task<T> CallDelayed<T>(Task<T> task)
        {
            await Task.Delay(Delay);
            T value = await task;
            return value;
        }

        private static async Task<(CardData Card, HttpResponseMessage Response)> GetCardDataFromOffers(HttpClient client, CardData cardData)
        {
            string url = $"{CardUrl}{cardData.UrlName}?{string.Join("&", Paramters)}";

            Debug.WriteLine($"GetCardDataFromOffers: {cardData.UrlName}");

            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {

                string content = await response.Content.ReadAsStringAsync();
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);

                HtmlNode titelNode = htmlDoc.DocumentNode.SelectSingleNode("/html/body/main/div[2]/div[@class='flex-grow-1']/h1");
                cardData.Name = titelNode.InnerText.Trim();

                HtmlNode imageNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='image card-image is-magic w-100 has-shadow']/img");
                if (imageNode != null)
                {
                    cardData.ImageUrl = imageNode.Attributes["src"].Value;
                }

                HtmlNode trendPriceNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"info\"]/div/dl/dd[4]/span");
                if (trendPriceNode != null)
                {
                    if (decimal.TryParse(trendPriceNode.InnerText.Replace("€", "").Trim(), out decimal price))
                    {
                        cardData.PriceTrend = price;
                    }
                }

                HtmlNodeCollection priceNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'price-container')]");
                if (priceNodes != null)
                {
                    cardData.Prices.Clear();
                    foreach (HtmlNode node in priceNodes)
                    {
                        HtmlNode priceSpan = node.SelectSingleNode(".//span[contains(@class, 'bold')]");
                        if (decimal.TryParse(priceSpan.InnerText.Replace("€", "").Trim(), out decimal price))
                        {
                            cardData.Prices.Add(price);
                        }
                    }
                }
            }

            return (cardData, response);
        }

        private static async Task<(CardData Card, HttpResponseMessage Response)> GetCardDataFromOffers(HttpClient client, string cardName)
        {
            return await GetCardDataFromOffers(client, new CardData(cardName));
        }

        private static async Task<(CardData Card, HttpResponseMessage Response)> SearchByCardName(HttpClient client, string cardName)
        {
            CardData card = new CardData(cardName);
            string name = HttpUtility.UrlEncode(Utilities.CleanCardName(cardName));
            string url = $"{SearchUrl}{name}";

            Debug.WriteLine($"SearchByCardName: {name}");

            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);

                HtmlNode searchResult = htmlDoc.DocumentNode.SelectSingleNode("/html/body/main/section/div[2]/div[1]/div[2]/div[1]/div[1]/h2/a");
                if (searchResult != null)
                {
                    card.UrlName = searchResult.Attributes["href"].Value.Split('/').Last();
                    Debug.WriteLine($"SearchByCardName: {card.UrlName}");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                }
            }
            else
            {
                card = null;
            }
            return (card, response);
        }

        private static void LogEvent(string message, int timeout = 0, bool done = false)
        {
            ReaderEventHandler?.Invoke(null, new ReaderEventArgs(message, timeout, done));
        }

    }

    public class ReaderEventArgs : EventArgs
    {
        public string Message { get; }
        public int Timeout { get; }
        public bool Done { get; }

        public ReaderEventArgs(string message, int timeout = 0, bool done = false)
        {
            Message = message;
            Timeout = timeout;
            Done = done;
        }
    }

}
