using Nop.Core;
using Square.Core.Enum;
using Square.Models;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Helpers for reading Square responses
/// </summary>
public static class SquareResponse
{
    /// <summary>
    /// Throws when a Square response carries errors
    /// </summary>
    /// <param name="errors">The "errors" member of the response</param>
    public static void ThrowIfErrors(IReadOnlyList<Error> errors)
    {
        if (!(errors?.Any() ?? false))
            return;

        var errorsMessage = string.Join(";", errors.Select(error => error.Detail));

        throw new NopException($"There are errors in the service response. {errorsMessage}");
    }

    /// <summary>
    /// Converts a value to the SDK enum member of the same wire value
    /// </summary>
    /// <typeparam name="TEnum">SDK enum type</typeparam>
    /// <param name="value">Wire value</param>
    /// <returns>The enum member</returns>
    public static TEnum ToEnum<TEnum>(string value) where TEnum : OpenStringEnum<TEnum>
    {
        if (OpenStringEnum<TEnum>.TryGetKnownValue(value, out var result))
            return result;

        throw new NopException($"'{value}' is not a value supported by Square ({typeof(TEnum).Name})");
    }
}
