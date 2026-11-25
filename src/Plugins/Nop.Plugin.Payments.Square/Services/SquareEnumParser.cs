using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts plain codes (nopCommerce currency and country codes) to the SDK's enum types, which have no public factory
/// </summary>
public static class SquareEnumParser
{
    /// <summary>
    /// Gets the currency Square declares for an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The currency</returns>
    public static Currency ParseCurrency(string code)
    {
        if (Currency.TryGetKnownValue(code, out var currency) && currency != null)
            return currency;

        throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the country Square declares for an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>The country; null when Square does not declare it</returns>
    public static Country? ParseCountry(string code)
    {
        return Country.TryGetKnownValue(code, out var country) ? country : null;
    }
}
