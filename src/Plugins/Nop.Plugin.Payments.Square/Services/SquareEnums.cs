using System.Text.Json;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Helpers for turning plain codes into the SDK's enum types, which have no public factory
/// </summary>
public static class SquareEnums
{
    /// <summary>
    /// Converts an ISO 4217 currency code to the SDK currency; a code the SDK does not declare is kept as it is
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The currency; null when the code is empty</returns>
    public static Currency ParseCurrency(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        return Currency.TryGetKnownValue(code.ToUpperInvariant(), out var known)
            ? known
            : JsonSerializer.Deserialize<Currency>(JsonSerializer.Serialize(code));
    }

    /// <summary>
    /// Converts an ISO 3166 two-letter country code to the SDK country; a code the SDK does not declare is kept as it is
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>The country; null when the code is empty</returns>
    public static Country ParseCountry(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        return Country.TryGetKnownValue(code.ToUpperInvariant(), out var known)
            ? known
            : JsonSerializer.Deserialize<Country>(JsonSerializer.Serialize(code));
    }
}
