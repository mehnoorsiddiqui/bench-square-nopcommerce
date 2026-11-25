using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts store codes into the values Square accepts
/// </summary>
public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency, so it cannot be sent</exception>
    public static Currency ToSquareCurrency(this string currencyCode)
    {
        return Currency.TryGetKnownValue(currencyCode, out var currency)
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166 alpha-2 country code
    /// </summary>
    /// <param name="countryCode">Country code</param>
    /// <returns>Square country; null when Square does not declare the country</returns>
    public static Country ToSquareCountry(this string countryCode)
    {
        return Country.TryGetKnownValue(countryCode, out var country) ? country : null;
    }

    #endregion
}
