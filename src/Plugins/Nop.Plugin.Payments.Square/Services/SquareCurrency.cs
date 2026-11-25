using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts store currency codes to Square currencies
/// </summary>
public static class SquareCurrency
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">Square does not know the currency</exception>
    public static Currency FromCode(string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency) || currency == null)
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }
}
