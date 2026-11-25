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
    /// Converts an ISO 4217 currency code to the Square currency
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    public static Currency ToCurrency(string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency))
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Converts a two-letter ISO 3166 country code to the Square country
    /// </summary>
    /// <param name="countryCode">Country code</param>
    /// <returns>The Square country; null when Square does not know the code</returns>
    public static Country ToCountry(string countryCode)
    {
        return Country.TryGetKnownValue(countryCode?.ToUpperInvariant(), out var country) ? country : null;
    }
}
