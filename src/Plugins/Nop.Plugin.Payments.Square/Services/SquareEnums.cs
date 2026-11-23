using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts the plain codes the store works with to the enum types of the Square SDK, which cannot be built
/// from a value the SDK does not declare
/// </summary>
public static class SquareEnums
{
    /// <summary>
    /// Gets the SDK currency of an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code; null or empty gives null</param>
    /// <returns>The currency; null when no code is given</returns>
    /// <exception cref="NopException">The SDK does not declare the currency</exception>
    public static Currency ToCurrency(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        return Currency.TryGetKnownValue(code.ToUpperInvariant(), out var currency)
            ? currency
            : throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the SDK country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>The country; null when the code is empty or the SDK does not declare the country</returns>
    public static Country ToCountry(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        return Country.TryGetKnownValue(code.ToUpperInvariant(), out var country) ? country : null;
    }
}
