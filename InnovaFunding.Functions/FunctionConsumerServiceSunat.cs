using InnovaFunding.Functions.Contract;
using InnovaFunding.Functions.Logic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

public class FunctionConsumerServiceSunat
{
    private readonly ILogger<FunctionConsumerServiceSunat> _logger;
    private readonly IDatabaseService _databaseService;

    public FunctionConsumerServiceSunat(ILogger<FunctionConsumerServiceSunat> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    [Function(nameof(FunctionConsumerServiceSunat))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        FunctionContext context)
    {
        var httpClient = new HttpClient();
        var url = "https://e-consulta.sunat.gob.pe/cl-at-ittipcam/tcS01Alias/listarTipoCambio";
        var scraper = new ConsumerServiceSunatLogic(httpClient, _databaseService, url);

        await scraper.InsertTipoCambioAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync("Tipo de cambio insertado correctamente.");

        _logger.LogInformation("HTTP trigger ejecutado correctamente.");
        return response;
    }
}