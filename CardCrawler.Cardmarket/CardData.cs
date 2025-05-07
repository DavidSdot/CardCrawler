using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace CardCrawler.Cardmarket
{
    public class CardData(string name)
    {
        public string Name { get; set; } = name;
        public string UrlName { get; set; } = Utilities.UrlEncodeCardName(Utilities.CleanCardName(name));

        public string ImageUrl { get; set; } = string.Empty;
        public Stream ImageStream { get; set; } = null;

        public decimal PriceTrend { get; set; }
        public decimal PriceAvg10 => Prices.Take(10).Average();
        public decimal PriceAvg50 => Prices.Take(50).Average();

        public List<decimal> Prices { get; set; } = [];

        public bool IsExcludedFromTotal { get; set; } = false;

    }
}
