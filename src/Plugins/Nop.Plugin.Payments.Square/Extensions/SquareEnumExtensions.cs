using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Resolves store codes onto the enums Square models use for them
/// </summary>
public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not know the currency, so no amount can be sent in it</exception>
    public static Currency ToSquareCurrency(this string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode, out var currency))
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166 alpha-2 country code
    /// </summary>
    /// <param name="countryCode">Country code</param>
    /// <returns>The Square country; null when Square does not know the code</returns>
    public static Country ToSquareCountry(this string countryCode)
    {
        return Country.TryGetKnownValue(countryCode, out var country) ? country : null;
    }

    #endregion
}
