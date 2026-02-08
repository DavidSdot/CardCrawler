using System.Collections.Generic;
using System.Text.Json.Serialization;

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
        public decimal? Avg { get; set; }
        public decimal? Low { get; set; }
        [JsonPropertyName("trend-foil")]
        public decimal? TrendFoil { get; set; }

        public decimal? BestPrice()
        {
            if (Trend.HasValue && Trend.Value > 0) { return Trend.Value; }
            if (Avg.HasValue && Avg.Value > 0) { return Avg.Value; }
            if (TrendFoil.HasValue && TrendFoil.Value > 0) { return TrendFoil.Value; }
            if (Low.HasValue && Low.Value > 0) { return Low.Value; }
            return null;
        }

    }
}
