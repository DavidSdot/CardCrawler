using System.Collections.Generic;

namespace CardCrawler.Cardmarket.Models
{
    public class CardMarketProductResponse
    {
        public List<CardMarketProduct>? Products { get; set; }
    }

    public class CardMarketProduct
    {
        public int IdProduct { get; set; }
        public string? Name { get; set; }
        public int IdCategory { get; set; }
        public string? CategoryName { get; set; }
    }

    public class CardMarketPriceResponse
    {
        public List<CardMarketPriceGuide>? PriceGuides { get; set; }
    }

    public class CardMarketPriceGuide
    {
        public int IdProduct { get; set; }
        public decimal? Trend { get; set; }
    }
}
