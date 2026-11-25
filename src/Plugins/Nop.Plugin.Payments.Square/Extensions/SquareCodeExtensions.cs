using Nop.Core;
using SquareEnums = Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts ISO codes stored in nopCommerce to the values Square accepts
/// </summary>
public static class SquareCodeExtensions
{
    #region Methods

    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">Square does not know the currency</exception>
    public static SquareEnums.Currency ToSquareCurrency(this string currencyCode)
    {
        if (!string.IsNullOrEmpty(currencyCode)
            && (SquareEnums.Currency.TryGetKnownValue(currencyCode, out var currency)
                || SquareEnums.Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out currency)))
        {
            return currency;
        }

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="countryCode">Two-letter country code, e.g. "US"</param>
    /// <returns>Square country; null when the code is empty or Square does not know it</returns>
    public static SquareEnums.Country ToSquareCountry(this string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
            return null;

        return SquareEnums.Country.TryGetKnownValue(countryCode, out var country)
            || SquareEnums.Country.TryGetKnownValue(countryCode.ToUpperInvariant(), out country)
            ? country
            : null;
    }

    #endregion
}
