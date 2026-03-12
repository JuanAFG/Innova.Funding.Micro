using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InnovaFunding.Functions;

public class FunctionConsumeMassiverServiceSunat
{
    private readonly ILogger<FunctionConsumeMassiverServiceSunat> _logger;

    public FunctionConsumeMassiverServiceSunat(ILogger<FunctionConsumeMassiverServiceSunat> logger)
    {
        _logger = logger;
    }

    [Function("FunctionConsumeMassiverServiceSunat")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}