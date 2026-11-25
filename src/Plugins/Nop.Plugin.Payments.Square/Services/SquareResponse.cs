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
    /// Converts a currency code to the currency Square declares
    /// </summary>
    /// <param name="code">ISO 4217 currency code</param>
    /// <returns>The Square currency</returns>
    public static Currency ToCurrency(string code)
    {
        //an undeclared currency cannot be sent: say so instead of sending a payload Square would refuse
        if (!Currency.TryGetKnownValue(code, out var currency) || currency == null)
            throw new NopException($"The {code} currency is not supported by the Square");

        return currency;
    }
}
