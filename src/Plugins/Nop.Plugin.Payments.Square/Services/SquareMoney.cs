using Nop.Core;
using Square.Models;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Builds Square money values from nopCommerce currency codes
/// </summary>
public static class SquareMoney
{
    /// <summary>
    /// Creates an amount of money
    /// </summary>
    /// <param name="amount">Amount in the smallest currency unit</param>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>The money; the currency is one the Square SDK declares, so a code it does not know is rejected here</returns>
    public static Money Create(long amount, string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode) || !Currency.TryGetKnownValue(currencyCode.ToUpperInvariant(), out var currency))
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return new Money { Amount = amount, Currency = currency };
    }
}
