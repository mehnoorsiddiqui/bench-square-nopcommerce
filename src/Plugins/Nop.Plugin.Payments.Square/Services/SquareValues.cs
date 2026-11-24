using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts store values (ISO codes) to the typed values the Square SDK expects
/// </summary>
public static class SquareValues
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not know the currency</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (!string.IsNullOrEmpty(currencyCode) && Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="twoLetterIsoCode">Country code, e.g. "US"</param>
    /// <returns>The Square country; null when the code is empty or Square does not know it, so the country is not sent</returns>
    public static Country ToCountry(string twoLetterIsoCode)
    {
        return !string.IsNullOrEmpty(twoLetterIsoCode) && Country.TryGetKnownValue(twoLetterIsoCode.ToUpperInvariant(), out var country)
            ? country
            : null;
    }
}
