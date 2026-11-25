using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Resolves store values (ISO codes) to the Square enums that the Square models type them as
/// </summary>
public static class SquareEnumHelper
{
    #region Methods

    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency ToSquareCurrency(string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode?.Trim().ToUpperInvariant(), out var currency) || currency is null)
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="twoLetterIsoCode">Two-letter country code</param>
    /// <returns>Square country; null when the code is empty or Square does not declare the country</returns>
    public static Country ToSquareCountry(string twoLetterIsoCode)
    {
        if (string.IsNullOrEmpty(twoLetterIsoCode))
            return null;

        return Country.TryGetKnownValue(twoLetterIsoCode.Trim().ToUpperInvariant(), out var country) ? country : null;
    }

    #endregion
}
