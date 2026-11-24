using Nop.Core;
using Square.Models;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Builds Square money amounts
/// </summary>
public static class SquareMoney
{
    /// <summary>
    /// Creates a Square money amount
    /// </summary>
    /// <param name="amount">Amount in the smallest currency unit</param>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>Money</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Money Create(long amount, string currencyCode)
    {
        //Square types the currency; a code it does not declare would be refused by Square anyway
        if (!Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency))
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return new Money { Amount = amount, Currency = currency };
    }
}
