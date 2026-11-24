using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts plain codes to the Square SDK's open enums, which have no conversion from string
/// </summary>
public static class SquareEnumExtensions
{
    /// <summary>
    /// Gets the Square currency for an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>Square currency</returns>
    public static Currency ToSquareCurrency(this string code)
    {
        if (Currency.TryGetKnownValue(code?.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country for an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>Square country; null if the code is not known to Square</returns>
    public static Country ToSquareCountry(this string code)
    {
        return Country.TryGetKnownValue(code?.ToUpperInvariant(), out var country) ? country : null;
    }
}
