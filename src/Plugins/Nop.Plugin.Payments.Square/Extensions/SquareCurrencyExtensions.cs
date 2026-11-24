using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

public static class SquareCurrencyExtensions
{
    #region Methods

    /// <summary>
    /// Resolves an ISO 4217 currency code to the SDK's Currency value
    /// </summary>
    /// <remarks>The SDK's enums have no public factory, so a code is resolved against the values the SDK declares</remarks>
    /// <param name="currencyCode">Currency code, such as "USD"</param>
    /// <returns>The SDK currency</returns>
    public static Currency ToSquareCurrency(this string currencyCode)
    {
        if (Currency.TryGetKnownValue(currencyCode?.Trim().ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"Currency '{currencyCode}' is not supported by Square");
    }

    /// <summary>
    /// Resolves an ISO 3166-1 alpha-2 country code to the SDK's Country value
    /// </summary>
    /// <param name="countryCode">Two-letter country code</param>
    /// <returns>The SDK country, or null when the SDK declares no such country</returns>
    public static Country? ToSquareCountry(this string countryCode)
    {
        return Country.TryGetKnownValue(countryCode?.Trim().ToUpperInvariant(), out var country) ? country : null;
    }

    #endregion
}
