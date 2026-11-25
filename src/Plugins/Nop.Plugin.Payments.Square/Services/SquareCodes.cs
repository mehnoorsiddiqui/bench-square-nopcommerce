using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts nopCommerce ISO codes to the code types Square declares
/// </summary>
public static class SquareCodes
{
    /// <summary>
    /// Gets the Square currency for an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (Currency.TryGetKnownValue(currencyCode?.Trim().ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country for an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="countryCode">Two-letter country code</param>
    /// <returns>The Square country; null when Square does not declare the code</returns>
    public static Country ToCountry(string countryCode)
    {
        return Country.TryGetKnownValue(countryCode?.Trim().ToUpperInvariant(), out var country) ? country : null;
    }
}
