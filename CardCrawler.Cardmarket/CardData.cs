using System.Collections.Generic;
using System.Linq;

namespace CardCrawler.Cardmarket
{
    public class CardData(string name)
    {
        public string Name { get; } = name;

        public string UrlName { get; set; } = string.Empty;

        public decimal PriceTrend { get; set; }
        public decimal PriceAvg10 => Prices.Take(10).Average();
        public decimal PriceAvg50 => Prices.Take(50).Average();

        public List<decimal> Prices { get; set; } = [];

    }
}
