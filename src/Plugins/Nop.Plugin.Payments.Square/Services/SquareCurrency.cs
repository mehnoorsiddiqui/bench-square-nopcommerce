using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts currency codes to the Square SDK currency type
/// </summary>
public static class SquareCurrency
{
    /// <summary>
    /// Gets the SDK currency of an ISO 4217 code; the SDK cannot hold a code it does not declare
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The SDK currency</returns>
    /// <exception cref="NopException">The SDK does not declare the currency</exception>
    public static Currency Parse(string code)
    {
        return Currency.TryGetKnownValue(code, out var currency) && currency != null
            ? currency
            : throw new NopException($"The {code} currency is not supported by the Square");
    }
}
