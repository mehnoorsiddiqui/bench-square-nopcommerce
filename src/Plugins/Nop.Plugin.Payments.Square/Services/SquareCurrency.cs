using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts currency codes to the currencies Square declares
/// </summary>
public static class SquareCurrency
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code, e.g. "USD"</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency FromCode(string currencyCode)
    {
        //the SDK's enums have no public factory for a value they do not declare
        if (currencyCode != null && Currency.TryGetKnownValue(currencyCode.Trim().ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }
}
