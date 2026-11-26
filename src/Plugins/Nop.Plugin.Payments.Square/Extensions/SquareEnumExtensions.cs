using Nop.Core;
using SquareCountry = Square.Models.Enums.Country;
using SquareCurrency = Square.Models.Enums.Currency;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts the ISO codes the store works with into the Square SDK's enums
/// </summary>
public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The SDK does not declare the currency, so a request cannot carry it</exception>
    public static SquareCurrency ToSquareCurrency(this string currencyCode)
    {
        if (SquareCurrency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency) && currency != null)
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="twoLetterIsoCode">ISO 3166-1 alpha-2 country code</param>
    /// <returns>The Square country; null when the SDK does not declare the country</returns>
    public static SquareCountry ToSquareCountry(this string twoLetterIsoCode)
    {
        return SquareCountry.TryGetKnownValue(twoLetterIsoCode?.ToUpperInvariant(), out var country) ? country : null;
    }

    #endregion
}
