using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Maps ISO codes onto the Square SDK's currency and country values
/// </summary>
public static class SquareCodes
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not know the currency</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        return Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency)
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="countryCode">Two-letter country code</param>
    /// <returns>The Square country; null when there is no code or Square does not know it</returns>
    public static Country ToCountry(string countryCode)
    {
        return Country.TryGetKnownValue(countryCode?.ToUpperInvariant(), out var country) ? country : null;
    }
}
