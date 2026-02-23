using InnovaFunding.Functions.Logic;
using InnovaFunding.Functions.Service;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace InnovaFunding.Functions.IntegrationTest
{
    public class FlowServiceSunatIntegrationTest
    {
        private readonly string _connectionString =
            "Server=tcp:ja-it-database-dev.database.windows.net;Database=jadbdev;User Id=ja-admin-db;Password=Azure2030@;";

        private SqlDatabaseService CreateDbService()
        {
            var inMemorySettings = new Dictionary<string, string>
                {
                    {"ConnectionStrings:InnovaFundingDb", _connectionString}
                };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            return new SqlDatabaseService(configuration);
        }

        [Fact]
        public async Task InsertTipoCambioAsync_ShouldInsertDataIntoDatabase()
        {
            string urlBase = "https://e-consulta.sunat.gob.pe/cl-at-ittipcam/tcS01Alias/listarTipoCambio";

            var httpClient = new HttpClient();
            var dbService = CreateDbService();
            var scraper = new ConsumerServiceSunatLogic(httpClient, dbService, urlBase);

            await scraper.InsertTipoCambioAsync();

            var today = DateTime.Now.Date.ToString("dd/MM/yyyy");
            var (priceSales, pricePurchase) = await dbService.GetYesterdayRateAsync(today);

            Assert.True(priceSales.HasValue && pricePurchase.HasValue,
                $"No se encontró tipo de cambio para {today}");
        }

        [Fact]
        public async Task InsertTipoCambioAsync_ShouldLogError_WhenApiFails()
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.sunat.gob.pe/fake-endpoint")
            };

            var dbService = CreateDbService();
            var scraper = new ConsumerServiceSunatLogic(httpClient, dbService, "https://api.sunat.gob.pe/fake-endpoint");

            await scraper.InsertTipoCambioAsync();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT TOP 1 ErrorMessage FROM ErrorLog ORDER BY CreatedDate DESC", connection);
            var lastError = (string?)await command.ExecuteScalarAsync();

            Assert.NotNull(lastError);
            Assert.Contains("Response status code does not indicate success", lastError);
        }

    }
}