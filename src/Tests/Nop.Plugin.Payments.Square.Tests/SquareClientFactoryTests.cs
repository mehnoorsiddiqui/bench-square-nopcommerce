using System.Net;
using System.Text;
using Moq;
using Nop.Plugin.Payments.Square.Extensions;
using Nop.Plugin.Payments.Square.Services;
using Nop.Services.Configuration;
using NUnit.Framework;
using Square.Models;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Tests;

/// <summary>
/// Remembers the requests it receives and answers each with an empty Square response
/// </summary>
public class RecordingHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
    }
}

[TestFixture]
public class SquareClientFactoryTests
{
    private static async Task<RecordingHandler> CallSquareAsync(SquarePaymentSettings settings)
    {
        var handler = new RecordingHandler();

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(factory => factory.CreateClient(SquarePaymentDefaults.SdkHttpClientName)).Returns(() => new HttpClient(handler, false));

        var settingService = new Mock<ISettingService>();
        settingService.Setup(service => service.LoadSettingAsync<SquarePaymentSettings>(It.IsAny<int>())).ReturnsAsync(settings);

        var client = await new SquareClientFactory(httpClientFactory.Object, settingService.Object).CreateAsync(1);
        await client.Locations.ListLocations();

        return handler;
    }

    [Test]
    public async Task Sandbox_CallsTheSandboxServerWithTheStoreTokenAndPluginUserAgent()
    {
        var handler = await CallSquareAsync(new SquarePaymentSettings { UseSandbox = true, AccessToken = "sandbox-token" });

        var request = handler.Requests.Single();
        Assert.That(request.RequestUri!.Host, Is.EqualTo("connect.squareupsandbox.com"));
        Assert.That(request.RequestUri.AbsolutePath, Is.EqualTo("/v2/locations"));
        Assert.That(request.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(request.Headers.Authorization.Parameter, Is.EqualTo("sandbox-token"));
        Assert.That(request.Headers.UserAgent.ToString(), Is.EqualTo(SquarePaymentDefaults.UserAgent));
    }

    [Test]
    public async Task Production_CallsTheProductionServer()
    {
        var handler = await CallSquareAsync(new SquarePaymentSettings { UseSandbox = false, AccessToken = "token" });

        Assert.That(handler.Requests.Single().RequestUri!.Host, Is.EqualTo("connect.squareup.com"));
    }

    [Test]
    public async Task NoStoreToken_IsNotReplacedByTheEnvironmentToken()
    {
        Environment.SetEnvironmentVariable("SQUARE_BEARER_AUTH", "token-of-another-account");
        try
        {
            var handler = await CallSquareAsync(new SquarePaymentSettings { UseSandbox = false, AccessToken = null });

            Assert.That(handler.Requests.Single().Headers.Authorization?.Parameter, Is.Not.EqualTo("token-of-another-account"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("SQUARE_BEARER_AUTH", null);
        }
    }
}

[TestFixture]
public class CreatePaymentRequestExtensionsTests
{
    [Test]
    public async Task WithIntegrationId_SendsTheFieldAndLeavesTheOriginalUnchanged()
    {
        var square = new StubSquare().Respond("POST /v2/payments", new CreatePaymentResponse { Payment = new Payment { Id = "P1", Status = "COMPLETED" } });
        var original = new CreatePaymentRequest
        {
            SourceId = "cnon:card",
            IdempotencyKey = "key",
            AmountMoney = new Money { Amount = 100, Currency = Currency.Usd }
        };

        var (payment, error) = await new SquarePaymentManager(square, null, null, null, null)
            .CreatePaymentAsync(original.WithIntegrationId("sqi_test"), 1);

        Assert.That(error, Is.Null);
        Assert.That(payment!.Id, Is.EqualTo("P1"));
        var body = System.Text.Json.JsonDocument.Parse(square.RequestBodies["POST /v2/payments"]).RootElement;
        Assert.That(body.GetProperty("integration_id").GetString(), Is.EqualTo("sqi_test"));
        Assert.That(body.GetProperty("source_id").GetString(), Is.EqualTo("cnon:card"));
        Assert.That(body.GetProperty("amount_money").GetProperty("currency").GetString(), Is.EqualTo("USD"));
        Assert.That(original.AdditionalProperties.ContainsKey("integration_id"), Is.False);
    }
}
