using Nop.Core;
using Square.Models;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Helpers for reading Square responses
/// </summary>
public static class SquareResponse
{
    /// <summary>
    /// Throws when a Square response carries errors
    /// </summary>
    /// <param name="errors">The "errors" member of the response</param>
    public static void ThrowIfErrors(IReadOnlyList<Error> errors)
    {
        if (!(errors?.Any() ?? false))
            return;

        var errorsMessage = string.Join(";", errors.Select(error => error.Detail));

        throw new NopException($"There are errors in the service response. {errorsMessage}");
    }

    /// <summary>
    /// Resolves an ISO 4217 currency code to the currency Square declares
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    public static Currency ParseCurrency(string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode, out var currency))
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Resolves an ISO 3166-1 alpha-2 country code to the country Square declares
    /// </summary>
    /// <param name="countryCode">Country code</param>
    /// <returns>The Square country; null when Square does not declare it</returns>
    public static Country ParseCountry(string countryCode)
    {
        return Country.TryGetKnownValue(countryCode, out var country) ? country : null;
    }
}
