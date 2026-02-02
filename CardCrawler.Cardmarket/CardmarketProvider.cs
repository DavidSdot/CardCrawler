using CardCrawler.Core.Interfaces;
using CardCrawler.Core.Models;
using System.Threading.Tasks;

namespace CardCrawler.Cardmarket
{
    public class CardmarketProvider : ICardDataProvider
    {
        public string SourceName => "Cardmarket";

        public async Task<bool> CheckConnection()
        {
            return await Reader.CheckConnection();
        }

        public async Task<CardData?> GetCardData(string cardName)
        {
            return await Reader.GetCardData(cardName);
        }
    }
}
