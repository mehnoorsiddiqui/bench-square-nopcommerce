using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts currency codes to the currencies Square's models declare
/// </summary>
public static class SquareCurrencyConverter
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square's models do not declare the currency</exception>
    public static Currency FromCode(string code)
    {
        if (!Currency.TryGetKnownValue(code, out var currency))
            throw new NopException($"The {code} currency is not supported by the Square");

        return currency;
    }
}
