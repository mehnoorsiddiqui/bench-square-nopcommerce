using Nop.Core;
using Square.Models;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Builds Square values whose codes the SDK declares as enums (currency, country)
/// </summary>
public static class SquareValues
{
    /// <summary>
    /// Creates an amount of money
    /// </summary>
    /// <param name="amount">Amount, in the smallest currency unit</param>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>The money</returns>
    /// <exception cref="NopException">The currency is not one Square declares</exception>
    public static Money CreateMoney(long amount, string currencyCode)
    {
        //an undeclared code cannot be built into the SDK's enum; Square would not accept it either
        if (!Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency) || currency == null)
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return new Money { Amount = amount, Currency = currency };
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="twoLetterIsoCode">Two-letter country code</param>
    /// <returns>The country; null when the code is empty or Square does not declare it</returns>
    public static Country GetCountry(string twoLetterIsoCode)
    {
        return Country.TryGetKnownValue(twoLetterIsoCode?.ToUpperInvariant(), out var country) ? country : null;
    }
}
