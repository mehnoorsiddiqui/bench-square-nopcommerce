using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Converts an ISO 4217 currency code to the currency Square declares
    /// </summary>
    /// <remarks>
    /// The SDK models currencies as a set of declared values and cannot be given a code it does not declare.
    /// </remarks>
    /// <param name="currencyCode">ISO 4217 currency code, e.g. "USD"</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The currency is not one Square supports</exception>
    public static Currency ToSquareCurrency(this string currencyCode)
    {
        if (!string.IsNullOrEmpty(currencyCode) && Currency.TryGetKnownValue(currencyCode, out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Converts an ISO 3166-1 alpha-2 country code to the country Square declares
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code, e.g. "US"</param>
    /// <returns>The Square country; null when Square does not declare it</returns>
    public static Country ToSquareCountry(this string countryCode)
    {
        return !string.IsNullOrEmpty(countryCode) && Country.TryGetKnownValue(countryCode, out var country) ? country : null;
    }

    #endregion
}
