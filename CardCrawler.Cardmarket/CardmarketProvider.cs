using CardCrawler.Browser;
using CardCrawler.Cardmarket.Models;
using CardCrawler.Core;
using CardCrawler.Core.Interfaces;
using CardCrawler.Core.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CardCrawler.Cardmarket
{
    public class CardMarketProvider : BaseCardProvider
    {
        public const string BaseUrl = "https://www.cardmarket.com";
        public const string CardUrl = BaseUrl + "/en/Magic/Cards/";
        public const string SearchUrl = BaseUrl + "/en/Magic/AdvancedSearch?doSubmit=1&cardName=";

        private readonly string[] Parameters =
        [
            "sellerCountry=1,2,3,33,35,5,6,8,9,11,12,7,14,15,37,16,17,21,18,19,20,22,23,24,25,26,27,29,31,30,10,28",
            "minCondition=4",
            "language=1,3"
        ];

        public override string SourceName => "Cardmarket";

        public CardMarketProvider() : base("cardmarket_prices.json")
        {
        }

        public override async Task<bool> CheckConnection()
        {
            if (await base.CheckConnection())
            {
                return true;
            }
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

        public override async Task UpdateLocalCardData()
        {
            string dir = Directory.GetCurrentDirectory();
            string productsFile = Path.Combine(dir, "products_singles_1.json");
            string priceFile = Path.Combine(dir, "price_guide_1.json");

            if (!File.Exists(productsFile) || !File.Exists(priceFile))
            {
                Console.WriteLine($"Could not find products_singles_1.json and price_guide_1.json in {dir}.");
                return;
            }

            var jOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            Console.WriteLine("Reading products...");
            string productsJson = await File.ReadAllTextAsync(productsFile);
            CardMarketProductResponse? productData = System.Text.Json.JsonSerializer.Deserialize<CardMarketProductResponse>(productsJson, jOptions);

            Console.WriteLine("Reading prices...");
            string pricesJson = await File.ReadAllTextAsync(priceFile);
            CardMarketPriceResponse? priceData = System.Text.Json.JsonSerializer.Deserialize<CardMarketPriceResponse>(pricesJson, jOptions);

            if (productData?.Products == null || priceData?.PriceGuides == null)
            {
                Console.WriteLine("Failed to parse Cardmarket data.");
                return;
            }

            Dictionary<int, CardMarketPriceGuide> priceLookup = priceData.PriceGuides.ToDictionary(p => p.IdProduct, p => p);
            List<CachedCard> cards = [];

            foreach (var product in productData.Products)
            {
                if (priceLookup.TryGetValue(product.IdProduct, out CardMarketPriceGuide? priceGuide))
                {
                    cards.Add(new CachedCard(product.IdProduct.ToString(), product.Name ?? "Unknown", priceGuide.BestPrice()));
                }
            }

            await File.WriteAllTextAsync(_cacheFile, System.Text.Json.JsonSerializer.Serialize(cards));
            Console.WriteLine($"Merged {cards.Count} cards to {_cacheFile}");
        }

        public override async Task<CardData?> GetCardData(string cardName)
        {
            CardData card = new(cardName);
            string urlName = Utilities.UrlEncodeCardName(card.Name);

            // Fast local lookup
            string key = cardName.ToLowerInvariant().Trim();
            if (_pricesByName.TryGetValue(key, out decimal? cachedPrice))
            {
                Debug.WriteLine($"_pricesByName:{key} > {cachedPrice}");
                return new CardData(cardName)
                {
                    PriceTrend = cachedPrice,
                    Url = $"{CardUrl}{urlName}"
                };
            }

            Debug.WriteLine($"Search by URL...{urlName}");
            string url = $"{CardUrl}{urlName}?{string.Join("&", Parameters)}";
            string? html = await Edge.GetPageContent(url);

            if (!string.IsNullOrWhiteSpace(html) && ParseCardPage(html) is CardData foundCard)
            {
                foundCard.UrlName = urlName;
                return foundCard;
            }

            Debug.WriteLine($"Search by Name...{card.Name}");
            if (await SearchByCardNameAsync(cardName) is CardData namedCard)
            {
                Debug.WriteLine($"Changed to: {namedCard.UrlName}...");
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
                if (string.IsNullOrWhiteSpace(urlName))
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

            Debug.WriteLine($"Card: {title}");

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

            return card;
        }

    }
}
