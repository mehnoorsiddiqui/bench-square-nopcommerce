using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Resolves plain codes (currency, country) to the enums the Square SDK declares for them
/// </summary>
public static class SquareLookups
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency GetCurrency(string currencyCode)
    {
        if (!string.IsNullOrEmpty(currencyCode) && Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code</param>
    /// <returns>The Square country; null when Square does not declare it</returns>
    public static Country GetCountry(string countryCode)
    {
        return !string.IsNullOrEmpty(countryCode) && Country.TryGetKnownValue(countryCode.ToUpperInvariant(), out var country)
            ? country
            : null;
    }
}
