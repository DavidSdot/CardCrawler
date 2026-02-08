namespace CardCrawler.Core.Models;

public class CardData(string name)
{
    public string Name { get; } = name;

    public int Count { get; set; } = 1;

    public string UrlName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public decimal? PriceTrend { get; set; }

}
