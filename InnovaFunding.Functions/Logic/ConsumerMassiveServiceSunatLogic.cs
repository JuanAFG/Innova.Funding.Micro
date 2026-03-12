using InnovaFunding.Functions.Contract;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace InnovaFunding.Functions.Logic
{
    public class ConsumerMassiveServiceSunatLogic
    {
        private readonly HttpClient _httpClient;
        private readonly IDatabaseService _databaseService;
        private readonly string _url;

        public ConsumerMassiveServiceSunatLogic(HttpClient httpClient, IDatabaseService databaseService, string url)
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

                int anioInicio = 2008;
                int anioFin = DateTime.Now.Year;
                int mesActual = DateTime.Now.Month;

                for (int anio = anioInicio; anio <= anioFin; anio++)
                {
                    for (int mes = 1; mes <= 12; mes++)
                    {
                        // Evitar consultar meses futuros del año actual
                        if (anio == anioFin && mes > mesActual)
                            break;

                        var payload = new
                        {
                            anio = anio,
                            mes = mes - 1,
                            token = Guid.NewGuid().ToString("N")
                        };

                        var json = JsonSerializer.Serialize(payload);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PostAsync(_url, content);
                        response.EnsureSuccessStatusCode();

                        var result = await response.Content.ReadAsStringAsync();

                        var tipoCambios = JsonSerializer.Deserialize<List<TipoCambioResponse>>(result);

                        if (tipoCambios == null || !tipoCambios.Any())
                            continue;

                        foreach (var group in tipoCambios.GroupBy(x => x.fecPublica))
                        {
                            var compra = group.FirstOrDefault(x => x.codTipo == "C");
                            var venta = group.FirstOrDefault(x => x.codTipo == "V");

                            decimal? pricePurchase = null;
                            decimal? priceSales = null;

                            if (compra != null && decimal.TryParse(compra.valTipo, NumberStyles.Any, CultureInfo.InvariantCulture, out var compraValue))
                                pricePurchase = compraValue;

                            if (venta != null && decimal.TryParse(venta.valTipo, NumberStyles.Any, CultureInfo.InvariantCulture, out var ventaValue))
                                priceSales = ventaValue;

                            await _databaseService.InsertRateAsync(
                                group.Key,
                                priceSales,
                                pricePurchase
                            );
                        }
                    }
                    Thread.Sleep(5000);
                }

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
