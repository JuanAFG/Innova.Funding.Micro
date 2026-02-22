using InnovaFunding.Functions.Contract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InnovaFunding.Functions.Logic
{
    public class ConsumerServiceSunatLogic
    {
        private readonly HttpClient _httpClient;
        private readonly IDatabaseService _databaseService;
        private readonly string _url;

        public ConsumerServiceSunatLogic(HttpClient httpClient, IDatabaseService databaseService, string url)
        {
            _httpClient = httpClient;
            _databaseService = databaseService;
            _url = url;


            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        }
        public async Task InsertTipoCambioAsync()
        {


            try
            {
                var payload = new
                {
                    anio = 2026,
                    mes = 1,
                    token = Guid.NewGuid().ToString("N")
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_url, content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var tipoCambios = JsonSerializer.Deserialize<List<TipoCambioResponse>>(result);

                var today = DateTime.Now.Date.ToString("dd/MM/yyyy");
                var typeChangeToday = tipoCambios.Where(x => x.fecPublica == today);

                double? priceSales = null;
                double? pricePurchase = null;
                string fecPublica = today;

                if (!typeChangeToday.Any())
                {
                    var yesterday = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
                    (priceSales, pricePurchase) = await _databaseService.GetYesterdayRateAsync(yesterday);
                    fecPublica = today;
                }
                else
                {
                    foreach (var tipoCambio in typeChangeToday)
                    {
                        fecPublica = tipoCambio.fecPublica;
                        if (tipoCambio.codTipo == "V")
                            priceSales = double.Parse(tipoCambio.valTipo, CultureInfo.InvariantCulture);
                        if (tipoCambio.codTipo == "C")
                            pricePurchase = double.Parse(tipoCambio.valTipo, CultureInfo.InvariantCulture);
                    }
                }

                await _databaseService.InsertRateAsync(fecPublica, priceSales, pricePurchase);
            }
            catch (Exception ex)
            {
                await _databaseService.LogErrorAsync(ex.Message, ex.StackTrace ?? string.Empty);
            }
        }

        public class TipoCambioResponse
        {
            public string fecPublica { get; set; }
            public string valTipo { get; set; }
            public string codTipo { get; set; }
        }
    }
}
