using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts plain codes (ISO currency and country codes) to the SDK's enum types, which cannot be created from an arbitrary string
/// </summary>
public static class SquareEnums
{
    #region Methods

    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The currency is not one Square declares</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (!string.IsNullOrEmpty(currencyCode)
            && Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out var currency)
            && currency != Currency.UnknownCurrency)
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="twoLetterIsoCode">Two-letter country code</param>
    /// <returns>The Square country; null when the code is empty or Square does not declare it</returns>
    public static Country ToCountry(string twoLetterIsoCode)
    {
        return !string.IsNullOrEmpty(twoLetterIsoCode) && Country.TryGetKnownValue(twoLetterIsoCode.ToUpperInvariant(), out var country)
            ? country
            : null;
    }

    #endregion
}
