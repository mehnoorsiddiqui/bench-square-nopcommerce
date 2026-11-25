using System.Text.Json;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Builds the SDK's open enums from the raw codes the plugin works with
/// </summary>
public static class SquareEnumExtensions
{
    /// <summary>
    /// Gets the SDK currency for an ISO 4217 code; a code the SDK does not declare is kept as is, so Square decides on it
    /// </summary>
    public static Currency ToSquareCurrency(this string code)
    {
        return JsonSerializer.Deserialize<Currency>(JsonSerializer.Serialize(code));
    }

    /// <summary>
    /// Gets the SDK country for an ISO 3166-1 alpha-2 code; null for an empty code
    /// </summary>
    public static Country ToSquareCountry(this string code)
    {
        return string.IsNullOrEmpty(code) ? null : JsonSerializer.Deserialize<Country>(JsonSerializer.Serialize(code));
    }
}
