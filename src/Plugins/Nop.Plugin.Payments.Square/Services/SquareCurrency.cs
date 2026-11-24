using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Helpers for Square currency codes
/// </summary>
public static class SquareCurrency
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency Parse(string currencyCode)
    {
        if (Currency.TryGetKnownValue(currencyCode, out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }
}
