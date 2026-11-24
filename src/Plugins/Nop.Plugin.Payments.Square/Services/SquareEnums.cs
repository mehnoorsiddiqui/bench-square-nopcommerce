using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Resolves the plain codes nopCommerce works with to the SDK's enum types, which have no public factory
/// </summary>
public static class SquareEnums
{
    /// <summary>
    /// Resolves an ISO 4217 currency code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The SDK currency</returns>
    /// <exception cref="NopException">The SDK does not declare the currency</exception>
    public static Currency ToCurrency(string code)
    {
        return Currency.TryGetKnownValue(code, out var currency)
            ? currency
            : throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Resolves an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>The SDK country; null when the SDK does not declare it</returns>
    public static Country ToCountry(string code)
    {
        return Country.TryGetKnownValue(code, out var country) ? country : null;
    }
}
