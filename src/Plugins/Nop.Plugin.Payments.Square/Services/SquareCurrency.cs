using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Helpers for the currencies the Square SDK declares
/// </summary>
public static class SquareCurrency
{
    /// <summary>
    /// Resolves an ISO 4217 currency code to the currency declared by the Square SDK
    /// </summary>
    /// <param name="code">ISO 4217 currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency Parse(string code)
    {
        if (Currency.TryGetKnownValue(code?.Trim().ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {code} currency is not supported by the Square");
    }
}
