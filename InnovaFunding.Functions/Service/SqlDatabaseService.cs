using InnovaFunding.Functions.Contract;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace InnovaFunding.Functions.Service
{
    public class SqlDatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;
        public SqlDatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<(double? PriceSales, double? PricePurchase)> GetYesterdayRateAsync(string date)
        {
            string _connectionString = _configuration.GetConnectionString("InnovaFundingDb");
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT PriceSales, Pricepurchase 
                  FROM SunatExchangeRate 
                  WHERE DatePublic = @DatePublic";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DatePublic", date);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetDouble(0), reader.GetDouble(1));
            }

            return (null, null);
        }

        public async Task InsertRateAsync(string datePublic, double? priceSales, double? pricePurchase)
        {
            string _connectionString = _configuration.GetConnectionString("InnovaFundingDb");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO [dbo].[SunatExchangeRate]
                  (DatePublic, PriceSales, Pricepurchase, CreatedDate, ModifiedDate)
                  VALUES (@DatePublic, @PriceSales, @Pricepurchase, @CreatedDate, @ModifiedDate)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DatePublic", datePublic);
            command.Parameters.AddWithValue("@PriceSales", priceSales ?? 0);
            command.Parameters.AddWithValue("@Pricepurchase", pricePurchase ?? 0);
            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }

        public async Task LogErrorAsync(string message, string stackTrace)
        {
            string _connectionString = _configuration.GetConnectionString("InnovaFundingDb");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("InsertErrorLog", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ErrorMessage", message);
            command.Parameters.AddWithValue("@StackTrace", stackTrace ?? string.Empty);
            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }
    }
}

