using CardCrawler.Core.Models;
using System.Threading.Tasks;

namespace CardCrawler.Core.Interfaces
{
    public interface ICardDataProvider
    {
        Task<CardData?> GetCardData(string cardName);
        Task<bool> CheckConnection();
        string SourceName { get; }
    }
}
