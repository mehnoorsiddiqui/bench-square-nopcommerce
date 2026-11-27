using System.Net;
using System.Text;
using System.Text.Json;
using Nop.Plugin.Payments.Square.Services;
using Square;
using Square.Core.Configuration;
using Square.Servers;

namespace Nop.Plugin.Payments.Square.Tests;

/// <summary>
/// Answers Square calls with canned SDK response objects and remembers the request bodies
/// </summary>
public class StubSquare : HttpMessageHandler, ISquareClientFactory
{
    private readonly Dictionary<string, (HttpStatusCode Status, object Body)> _responses = new();

    public Dictionary<string, string> RequestBodies { get; } = new();

    /// <summary>
    /// Every call received, in order, e.g. "POST /v2/payments/P1/cancel"
    /// </summary>
    public List<string> Calls { get; } = new();

    /// <summary>
    /// Sets the response of a call, e.g. Respond("GET /v2/customers/C1", new RetrieveCustomerResponse { ... })
    /// </summary>
    public StubSquare Respond(string call, object body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses[call] = (status, body);
        return this;
    }

    public T RequestBody<T>(string call) => JsonSerializer.Deserialize<T>(RequestBodies[call]);

    public Task<SquareClient> CreateAsync(int storeId) => Task.FromResult(new SquareClient(new HttpClient(this, false), new SquareClientOptions
    {
        BearerAuth = "test",
        Environment = ServerEnvironment.Production,
        Retry = RetryOptions.Disabled()
    }));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var call = $"{request.Method} {request.RequestUri!.AbsolutePath}";
        Calls.Add(call);
        if (request.Content != null)
            RequestBodies[call] = await request.Content.ReadAsStringAsync(cancellationToken);

        if (!_responses.TryGetValue(call, out var response))
            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };

        return new HttpResponseMessage(response.Status)
        {
            Content = new StringContent(JsonSerializer.Serialize(response.Body), Encoding.UTF8, "application/json")
        };
    }
}
