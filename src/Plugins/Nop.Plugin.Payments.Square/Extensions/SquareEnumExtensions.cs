using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts raw codes into the SDK's enum values (an enum value cannot be built from an arbitrary string)
/// </summary>
public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Gets the SDK currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>The currency; null when the code is empty</returns>
    /// <exception cref="NopException">The currency is not one the SDK declares</exception>
    public static Currency ToSquareCurrency(this string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode))
            return null;

        if (Currency.TryGetKnownValue(currencyCode, out var currency)
            || Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the SDK country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="twoLetterIsoCode">Country code, e.g. "US"</param>
    /// <returns>The country; null when the code is empty or not one the SDK declares</returns>
    public static Country ToSquareCountry(this string twoLetterIsoCode)
    {
        if (string.IsNullOrEmpty(twoLetterIsoCode))
            return null;

        return Country.TryGetKnownValue(twoLetterIsoCode.ToUpperInvariant(), out var country) ? country : null;
    }

    #endregion
}
