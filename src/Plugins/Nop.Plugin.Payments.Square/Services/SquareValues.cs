using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Resolves store values to the values Square declares
/// </summary>
public static class SquareValues
{
    /// <summary>
    /// Gets the Square currency of a currency code
    /// </summary>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode, out var currency) || currency == null)
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Gets the Square country of a country code
    /// </summary>
    /// <param name="twoLetterIsoCode">ISO 3166-1 alpha-2 country code</param>
    /// <returns>The Square country; null when the code is empty or Square does not declare it</returns>
    public static Country ToCountry(string twoLetterIsoCode)
    {
        return Country.TryGetKnownValue(twoLetterIsoCode, out var country) ? country : null;
    }
}
