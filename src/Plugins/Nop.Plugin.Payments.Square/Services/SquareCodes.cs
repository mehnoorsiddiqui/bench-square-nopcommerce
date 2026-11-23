using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts the ISO codes the store works with into the enum values the Square SDK expects
/// </summary>
public static class SquareCodes
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>The Square currency; null when the code is empty</returns>
    /// <exception cref="NopException">The currency is not one Square declares</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode))
            return null;

        if (!Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out var currency))
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="countryCode">Two-letter country code, e.g. "US"</param>
    /// <returns>The Square country; null when the code is empty or not one Square declares</returns>
    public static Country ToCountry(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
            return null;

        return Country.TryGetKnownValue(countryCode.ToUpperInvariant(), out var country) ? country : null;
    }
}
