using System.Text.Json;

namespace Nop.Plugin.Payments.Square.Extensions;

public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Converts a wire value (e.g. a currency or country code) to the Square SDK enum of the given type
    /// </summary>
    /// <remarks>
    /// The SDK enums (Currency, Country, ...) have no public constructor: they are built from a value by the SDK's own
    /// JSON converter, which also accepts values the SDK does not list (open enums), as the API does.
    /// </remarks>
    /// <typeparam name="TEnum">The Square SDK enum type</typeparam>
    /// <param name="value">Wire value</param>
    /// <returns>The enum; null when the value is null</returns>
    public static TEnum ToSquareEnum<TEnum>(this string value) where TEnum : class
    {
        return value == null ? null : JsonSerializer.Deserialize<TEnum>(JsonSerializer.Serialize(value));
    }

    #endregion
}
