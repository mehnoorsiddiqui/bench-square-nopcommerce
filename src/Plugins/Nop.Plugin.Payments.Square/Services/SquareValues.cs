using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts plain values (currency and country codes) to the enums the Square SDK types them with
/// </summary>
public static class SquareValues
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The currency is not one Square declares</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (Currency.TryGetKnownValue(currencyCode?.Trim().ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="twoLetterIsoCode">Country code</param>
    /// <returns>The Square country; null when Square does not declare the country</returns>
    public static Country ToCountry(string twoLetterIsoCode)
    {
        return Country.TryGetKnownValue(twoLetterIsoCode?.Trim().ToUpperInvariant(), out var country) ? country : null;
    }
}
