using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts nopCommerce currency and country codes to the Square SDK enums
/// </summary>
/// <remarks>
/// Square SDK enums have no factory for a value the SDK does not declare, so a code is resolved against the known values
/// </remarks>
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
        return Currency.TryGetKnownValue(currencyCode?.Trim().ToUpperInvariant(), out var currency)
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="countryCode">Two-letter country code</param>
    /// <returns>Square country; null when the code is empty or Square does not know it</returns>
    public static Country ToCountry(string countryCode)
    {
        return Country.TryGetKnownValue(countryCode?.Trim().ToUpperInvariant(), out var country) ? country : null;
    }
}
