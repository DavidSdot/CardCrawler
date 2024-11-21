using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CardCrawler.Cardmarket
{
    public class CardData
    {
        public CardData(string name)
        {
            Name = name;
            UrlName = Utilities.UrlEncodeCardName(Utilities.CleanCardName(name));
        }

        public string Name { get; set; }
        public string UrlName { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
        public Stream ImageStream { get; set; } = null;

        public decimal PriceTrend { get; set; }
        public decimal PriceAvg10 => Prices.Take(10).Average();
        public decimal PriceAvg50 => Prices.Take(50).Average();

        public List<decimal> Prices { get; set; } = new List<decimal>();

    }
}
