using InnovaFunding.Functions.Logic;
using InnovaFunding.Functions.Service;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace InnovaFunding.Functions.IntegrationTest
{
    public class FlowServiceSunatIntegrationTest
    {
        private IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .AddUserSecrets<FlowServiceSunatIntegrationTest>() // local
                .AddEnvironmentVariables() // CI/CD
                .Build();
        }
        private SqlDatabaseService CreateDbService()
        {
            var configuration = BuildConfiguration();
            return new SqlDatabaseService(configuration);
        }


        private string GetConnectionString()
        {
            var configuration = BuildConfiguration();
            var conn = configuration.GetConnectionString("InnovaFundingDb");

            if (string.IsNullOrEmpty(conn))
                throw new InvalidOperationException("No se encontró la cadena de conexión 'InnovaFundingDb'. Revisa UserSecrets o variables de entorno.");

            return conn;
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

            // Usa la cadena desde el secret
            var connectionString = GetConnectionString();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT TOP 1 ErrorMessage FROM ErrorLog ORDER BY CreatedDate DESC", connection);
            var lastError = (string?)await command.ExecuteScalarAsync();

            Assert.NotNull(lastError);
            Assert.Contains("Response", lastError); // validación más flexible
        }
    }
}