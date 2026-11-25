using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts store codes (ISO currency and country codes) to the Square SDK's enum values
/// </summary>
public static class SquareCodes
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">The currency is not one Square knows</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166 two-letter country code
    /// </summary>
    /// <param name="twoLetterIsoCode">Two-letter country code</param>
    /// <returns>Square country; null when the code is not one Square knows</returns>
    public static Country ToCountry(string twoLetterIsoCode)
    {
        return Country.TryGetKnownValue(twoLetterIsoCode?.ToUpperInvariant(), out var country) ? country : null;
    }
}
