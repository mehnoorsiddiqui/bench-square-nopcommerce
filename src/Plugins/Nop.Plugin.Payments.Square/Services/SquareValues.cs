using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts store values to the values Square declares
/// </summary>
public static class SquareValues
{
    /// <summary>
    /// Converts a currency code to the Square currency
    /// </summary>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        return Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency) && currency is not null
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Converts a two-letter ISO country code to the Square country
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code</param>
    /// <returns>Square country; null when Square does not declare it</returns>
    public static Country ToCountry(string countryCode)
    {
        return Country.TryGetKnownValue(countryCode?.ToUpperInvariant(), out var country) ? country : null;
    }
}
