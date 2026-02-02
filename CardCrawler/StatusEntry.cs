namespace CardCrawler
{
    public class StatusEntry(string name)
    {
        public string Name { get; set; } = name;
        public string Symbol { get; set; } = " ";
        public string Price { get; set; } = "--";
        public string Info { get; set; } = "waiting";
        public int Count { get; set; } = 1;
        public bool ExceedsLimit { get; set; }
    }
}
