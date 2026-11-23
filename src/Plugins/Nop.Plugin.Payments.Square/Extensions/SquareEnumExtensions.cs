using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts nopCommerce codes to the enums Square's models declare
/// </summary>
public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The currency is not one Square declares</exception>
    public static Currency ToSquareCurrency(this string currencyCode)
    {
        if (Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency) && currency != null)
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="twoLetterIsoCode">Country code</param>
    /// <returns>The Square country; null when Square does not declare the country</returns>
    public static Country ToSquareCountry(this string twoLetterIsoCode)
    {
        return Country.TryGetKnownValue(twoLetterIsoCode?.ToUpperInvariant(), out var country) ? country : null;
    }

    #endregion
}
