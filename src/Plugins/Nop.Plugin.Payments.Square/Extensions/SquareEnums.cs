using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Turns the codes the store works with (ISO 4217 currency, ISO 3166 country) into the SDK's enum values.
/// The SDK has no public way to build a value it does not declare, so a code is looked up among the declared ones.
/// </summary>
public static class SquareEnums
{
    #region Methods

    /// <summary>
    /// Gets the SDK currency of an ISO 4217 code
    /// </summary>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>The SDK currency</returns>
    /// <exception cref="NopException">The code is not a currency Square declares</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (!string.IsNullOrEmpty(currencyCode) && Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the SDK country of an ISO 3166 two-letter code
    /// </summary>
    /// <param name="countryCode">Country code, e.g. "US"</param>
    /// <returns>The SDK country; null when the code is empty or not a country Square declares</returns>
    public static Country ToCountry(string countryCode)
    {
        return !string.IsNullOrEmpty(countryCode) && Country.TryGetKnownValue(countryCode.ToUpperInvariant(), out var country)
            ? country
            : null;
    }

    #endregion
}
