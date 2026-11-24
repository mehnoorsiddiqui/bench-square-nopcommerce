using Nop.Core;
using Square.Models;
using Square.Models.Enums;

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
    /// Converts a currency code to the SDK currency
    /// </summary>
    /// <param name="code">ISO 4217 currency code</param>
    /// <returns>The SDK currency</returns>
    public static Currency ToCurrency(string code)
    {
        if (!Currency.TryGetKnownValue(code, out var currency))
            throw new NopException($"Currency {code} is not supported by Square");

        return currency;
    }
}
