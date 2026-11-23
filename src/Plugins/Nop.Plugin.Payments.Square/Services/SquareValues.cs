using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts plain values to the enums the Square SDK types them with
/// </summary>
public static class SquareValues
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The code is not a currency Square declares</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode, out var currency))
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="twoLetterIsoCode">Country code</param>
    /// <returns>The Square country; null when the code is empty or not a country Square declares</returns>
    public static Country ToCountry(string twoLetterIsoCode)
    {
        return Country.TryGetKnownValue(twoLetterIsoCode, out var country) ? country : null;
    }
}
