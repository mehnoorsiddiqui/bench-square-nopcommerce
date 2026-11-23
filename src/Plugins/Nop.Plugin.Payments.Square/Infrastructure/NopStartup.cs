using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.Square.Services;
using Nop.Web.Framework.Infrastructure.Extensions;

namespace Nop.Plugin.Payments.Square.Infrastructure;

/// <summary>
/// Represents object for the configuring services on application startup
/// </summary>
public class NopStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISquareClientFactory, SquareClientFactory>();
        services.AddScoped<SquarePaymentManager>();
        services.AddScoped<SquareCardService>();
        services.AddScoped<LegacyTransactionService>();
        services.AddScoped<SquareInventoryService>();
        services.AddScoped<SquareCatalogService>();
        services.AddScoped<SquareCustomerLinkService>();

        //client to request Square authorization service
        services.AddHttpClient<SquareAuthorizationHttpClient>().WithProxy();

        //HttpClient for the Square SDK (bounded attempts, store proxy settings); taken from the factory
        //for every SDK client, so the factory's handler rotation keeps connections and DNS fresh
        //the SDK owns the timeouts (RetryOptions.Timeout bounds each attempt and a timed-out attempt is retried), so the
        //transport must not race it: a transport-level timeout surfaces as the same error but cannot be retried
        services.AddHttpClient(SquarePaymentDefaults.SdkHttpClientName, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .WithProxy();
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => 101;
}