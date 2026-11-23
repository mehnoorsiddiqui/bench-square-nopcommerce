using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts plugin values (ISO codes) into the Square SDK's enum values
/// </summary>
public static class SquareEnums
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="code">ISO 4217 currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The currency is not one Square knows</exception>
    public static Currency ToCurrency(string code)
    {
        if (!string.IsNullOrEmpty(code) && Currency.TryGetKnownValue(code.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="code">ISO 3166-1 alpha-2 country code</param>
    /// <returns>The Square country; null when Square does not know the code</returns>
    public static Country ToCountry(string code)
    {
        return !string.IsNullOrEmpty(code) && Country.TryGetKnownValue(code.ToUpperInvariant(), out var country) ? country : null;
    }
}
