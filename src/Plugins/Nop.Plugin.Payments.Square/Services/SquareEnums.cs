using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts plain codes to the SDK's enum types
/// </summary>
public static class SquareEnums
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency ToCurrency(string code)
    {
        return Currency.TryGetKnownValue(code, out var currency)
            ? currency
            : throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>The Square country; null when Square does not declare it</returns>
    public static Country? ToCountryOrNull(string code)
    {
        return Country.TryGetKnownValue(code, out var country) ? country : null;
    }
}
