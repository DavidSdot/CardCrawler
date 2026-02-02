namespace CardCrawler
{
    public class StatusEntry(string name)
    {
        public string Name { get; set; } = name;
        public string Symbol { get; set; } = " ";
        public string Info { get; set; } = "";
        public int Count { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public bool ExceedsLimit { get; set; }
    }
}
