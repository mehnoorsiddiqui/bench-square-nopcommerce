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
    /// Resolves a currency code to the currency the Square SDK declares
    /// </summary>
    /// <param name="currencyCode">ISO 4217 currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The SDK declares no such currency, so Square cannot be asked to use it</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        return Currency.TryGetKnownValue(currencyCode, out var currency)
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }
}
