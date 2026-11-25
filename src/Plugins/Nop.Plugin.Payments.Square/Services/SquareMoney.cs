using Nop.Core;
using Square.Models;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Helpers for building Square money amounts
/// </summary>
public static class SquareMoney
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency ParseCurrency(string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode?.Trim().ToUpperInvariant(), out var currency))
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Creates a Square money amount
    /// </summary>
    /// <param name="amount">Amount in the smallest currency unit</param>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>Square money amount</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Money Create(long amount, string currencyCode)
    {
        return new Money { Amount = amount, Currency = ParseCurrency(currencyCode) };
    }
}
