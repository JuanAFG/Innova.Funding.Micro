
namespace InnovaFunding.Functions.Contract
{
    public interface IDatabaseService
    {
        Task<(double? PriceSales, double? PricePurchase)> GetYesterdayRateAsync(string date);
        Task InsertRateAsync(string datePublic, double? priceSales, double? pricePurchase);
        Task LogErrorAsync(string message, string stackTrace);
    }
}
