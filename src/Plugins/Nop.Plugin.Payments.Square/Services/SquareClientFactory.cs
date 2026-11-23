using Microsoft.Extensions.Logging.Abstractions;
using Nop.Core;
using Nop.Services.Configuration;
using Square;
using Square.Core.Configuration;
using Square.Core.Hooks;
using Square.Servers;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Creates Square SDK clients for a store
/// </summary>
public interface ISquareClientFactory
{
    /// <summary>
    /// Creates the Square client configured for the store
    /// </summary>
    /// <param name="storeId">Store identifier for which configuration should be loaded</param>
    /// <returns>The asynchronous task whose result contains the Square client</returns>
    Task<SquareClient> CreateAsync(int storeId);
}

/// <summary>
/// Creates Square SDK clients from the per-store plugin settings
/// </summary>
public class SquareClientFactory : ISquareClientFactory
{
    #region Fields

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public SquareClientFactory(IHttpClientFactory httpClientFactory, ISettingService settingService)
    {
        _httpClientFactory = httpClientFactory;
        _settingService = settingService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates the Square client configured for the store
    /// </summary>
    /// <param name="storeId">Store identifier for which configuration should be loaded</param>
    /// <returns>The asynchronous task whose result contains the Square client</returns>
    public async Task<SquareClient> CreateAsync(int storeId)
    {
        var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeId);

        //validate access token
        if (settings.UseSandbox && string.IsNullOrEmpty(settings.AccessToken))
            throw new NopException("Sandbox access token should not be empty");

        //settings are per store and read on every call, so the client is built per call;
        //the HttpClient comes from the factory (store proxy settings; its attempts are bounded by the SDK's retry timeout)
        var options = new SquareClientOptions
        {
            BearerAuth = settings.AccessToken,
            Environment = settings.UseSandbox ? ServerEnvironment.Sandbox : ServerEnvironment.Production,
            Retry = RetryOptions.Default() with { Timeout = SquarePaymentDefaults.RequestTimeout },
            //set explicitly: an unset factory lets the SQUARE_LOG variable log request bodies (card nonces, addresses)
            Logging = new LoggingOptions { LoggerFactory = NullLoggerFactory.Instance },
            Hooks =
            [
                SdkHook.OnRequest((request, _) =>
                {
                    request.Headers.Remove("User-Agent");
                    request.Headers.TryAddWithoutValidation("User-Agent", SquarePaymentDefaults.UserAgent);
                })
            ]
        };

        return new SquareClient(_httpClientFactory.CreateClient(SquarePaymentDefaults.SdkHttpClientName), options);
    }

    #endregion
}
