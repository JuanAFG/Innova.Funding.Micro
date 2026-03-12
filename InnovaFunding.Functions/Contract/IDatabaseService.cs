
namespace InnovaFunding.Functions.Contract
{
    public interface IDatabaseService
    {
        Task<(decimal? PriceSales, decimal? PricePurchase)> GetYesterdayRateAsync(string date);
        Task InsertRateAsync(string datePublic, decimal? priceSales, decimal? pricePurchase);
        Task LogErrorAsync(string message, string stackTrace);
    }
}
