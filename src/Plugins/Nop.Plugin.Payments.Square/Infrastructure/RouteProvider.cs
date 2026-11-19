using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Square.Infrastructure;

/// <summary>
/// Represents plugin route provider
/// </summary>
public class RouteProvider : IRouteProvider
{
    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        //add route for the access token callback
        endpointRouteBuilder.MapControllerRoute(name: SquarePaymentDefaults.AccessTokenRoute,
            pattern: "Plugins/PaymentSquare/AccessToken/",
            defaults: new { controller = "PaymentSquare", action = "AccessTokenCallback" });

        //add route for the customer's saved cards page
        endpointRouteBuilder.MapControllerRoute(name: SquarePaymentDefaults.SavedCardsRoute,
            pattern: "customer/square-cards",
            defaults: new { controller = "PaymentSquare", action = "SavedCards" });
    }

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 0;
}