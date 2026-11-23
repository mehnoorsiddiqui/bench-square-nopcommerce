using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Resolves the plain codes the store works with into the Square SDK's enums
/// </summary>
public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Resolves an ISO 4217 currency code into the Square currency
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency; null when the code is empty</returns>
    /// <exception cref="NopException">The currency is not one Square declares</exception>
    public static Currency ToSquareCurrency(this string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode))
            return null;

        return Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out var currency)
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Resolves an ISO 3166-1 alpha-2 country code into the Square country
    /// </summary>
    /// <param name="countryCode">Two-letter country code</param>
    /// <returns>The Square country; null when the code is empty or Square does not declare it</returns>
    public static Country ToSquareCountry(this string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
            return null;

        return Country.TryGetKnownValue(countryCode.ToUpperInvariant(), out var country) ? country : null;
    }

    #endregion
}
