using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts codes known to nopCommerce into the SDK's typed values
/// </summary>
public static class SquareValues
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The SDK does not declare the currency, so a request cannot carry it</exception>
    public static Currency ToCurrency(string code)
    {
        if (!string.IsNullOrEmpty(code) && Currency.TryGetKnownValue(code.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>The Square country; null when the SDK does not declare it</returns>
    public static Country ToCountry(string code)
    {
        return !string.IsNullOrEmpty(code) && Country.TryGetKnownValue(code.ToUpperInvariant(), out var country) ? country : null;
    }
}
