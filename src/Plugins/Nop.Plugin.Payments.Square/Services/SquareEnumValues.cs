using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts store codes to the typed values Square models carry
/// </summary>
public static class SquareEnumValues
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not know the currency</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency) || currency == null)
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="countryCode">Two-letter country code</param>
    /// <returns>The Square country; null when the code is empty or Square does not know the country</returns>
    public static Country ToCountry(string countryCode)
    {
        return Country.TryGetKnownValue(countryCode?.ToUpperInvariant(), out var country) ? country : null;
    }
}
