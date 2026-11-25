using Nop.Core;
using Square.Models;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Builds Square money amounts from store values
/// </summary>
public static class SquareMoney
{
    /// <summary>
    /// Creates a Square money amount
    /// </summary>
    /// <param name="amount">Amount in the smallest currency unit</param>
    /// <param name="currencyCode">ISO 4217 currency code, as Square writes it (e.g. "USD")</param>
    /// <returns>Square money amount</returns>
    /// <exception cref="NopException">Square does not know the currency</exception>
    public static Money Create(long amount, string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode, out var currency))
            throw new NopException($"Currency '{currencyCode}' is not supported by Square");

        return new Money { Amount = amount, Currency = currency };
    }
}
