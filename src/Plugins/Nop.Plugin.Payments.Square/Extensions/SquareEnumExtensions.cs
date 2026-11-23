using Nop.Core;
using SquareEnums = Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts the plain ISO codes the store works with to the Square SDK's enum types
/// </summary>
public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Converts an ISO 4217 currency code to the currency Square declares
    /// </summary>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static SquareEnums.Currency ToSquareCurrency(this string currencyCode)
    {
        if (!string.IsNullOrEmpty(currencyCode) && SquareEnums.Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Converts an ISO 3166-1 alpha-2 country code to the country Square declares
    /// </summary>
    /// <param name="countryCode">Two-letter country code, e.g. "US"</param>
    /// <returns>The Square country; null when Square does not declare it</returns>
    public static SquareEnums.Country ToSquareCountry(this string countryCode)
    {
        return !string.IsNullOrEmpty(countryCode) && SquareEnums.Country.TryGetKnownValue(countryCode.ToUpperInvariant(), out var country)
            ? country
            : null;
    }

    #endregion
}
