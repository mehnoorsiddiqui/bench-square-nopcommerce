using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Maps nopCommerce ISO codes to the values Square accepts
/// </summary>
public static class SquareCodes
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">Square does not know the currency</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        return Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency)
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166 two-letter country code
    /// </summary>
    /// <param name="countryCode">Two-letter country code</param>
    /// <returns>Square country; null when Square does not know the country</returns>
    public static Country ToCountry(string countryCode)
    {
        return Country.TryGetKnownValue(countryCode?.ToUpperInvariant(), out var country) ? country : null;
    }
}
