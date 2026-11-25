using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Maps ISO codes onto the SDK's typed enums
/// </summary>
public static class SquareCurrency
{
    /// <summary>
    /// Gets the SDK currency of an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The currency; null when the code is empty</returns>
    /// <exception cref="NopException">The SDK declares no such currency, so a request cannot carry it</exception>
    public static Currency Resolve(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        return Currency.TryGetKnownValue(code.ToUpperInvariant(), out var currency)
            ? currency
            : throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the SDK country of an ISO 3166 alpha-2 code
    /// </summary>
    /// <param name="code">Two-letter country code</param>
    /// <returns>The country; null when the SDK declares no such country</returns>
    public static Country ResolveCountry(string code)
    {
        return !string.IsNullOrEmpty(code) && Country.TryGetKnownValue(code.ToUpperInvariant(), out var country) ? country : null;
    }
}
