using InnovaFunding.Functions.Contract;
using InnovaFunding.Functions.Logic;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InnovaFunding.Functions.UnitTesting;

public class FlowServiceSunatUnitTest
{
    private readonly string _url = "https://e-consulta.sunat.gob.pe/cl-at-ittipcam/tcS01Alias/listarTipoCambio";

    [Fact]
    public async Task InsertTipoCambioAsync_ShouldInsertTodayRate_WhenAvailable()
    {
        // Arrange
        var today = DateTime.Now.Date.ToString("dd/MM/yyyy");
        var fakeResponse = new List<ConsumerServiceSunatLogic.TipoCambioResponse>
        {
            new ConsumerServiceSunatLogic.TipoCambioResponse { fecPublica = today, codTipo = "V", valTipo = "3.85" },
            new ConsumerServiceSunatLogic.TipoCambioResponse { fecPublica = today, codTipo = "C", valTipo = "3.80" }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(fakeResponse), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        var dbMockRun = new Mock<IDatabaseService>();
        var service = new ConsumerServiceSunatLogic(httpClient, dbMockRun.Object, _url);

        // Act
        await service.InsertTipoCambioAsync();

        // Assert
        dbMockRun.Verify(d => d.InsertRateAsync(today, 3.85, 3.80), Times.Once);
    }

    [Fact]
    public async Task InsertTipoCambioAsync_ShouldFallbackToYesterday_WhenNoTodayRate()
    {
        // Arrange
        var fakeResponse = new List<ConsumerServiceSunatLogic.TipoCambioResponse>(); // vacío
        var yesterday = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(fakeResponse), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        var dbMock = new Mock<IDatabaseService>();
        dbMock.Setup(d => d.GetYesterdayRateAsync(yesterday))
              .ReturnsAsync((3.70, 3.65));

        var service = new ConsumerServiceSunatLogic(httpClient, dbMock.Object, _url);

        // Act
        await service.InsertTipoCambioAsync();

        // Assert
        dbMock.Verify(d => d.InsertRateAsync(It.IsAny<string>(), 3.70, 3.65), Times.Once);
    }

    [Fact]
    public async Task InsertTipoCambioAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Simulated error"));

        var httpClient = new HttpClient(handlerMock.Object);

        var dbMock = new Mock<IDatabaseService>();
        var service = new ConsumerServiceSunatLogic(httpClient, dbMock.Object, _url);

        // Act
        await service.InsertTipoCambioAsync();

        // Assert
        dbMock.Verify(d => d.LogErrorAsync("Simulated error", It.IsAny<string>()), Times.Once);
    }
}
