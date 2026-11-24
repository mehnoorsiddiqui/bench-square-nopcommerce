using Nop.Core;
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
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    public static global::Square.Models.Enums.Currency ToCurrency(string currencyCode)
    {
        return global::Square.Models.Enums.Currency.TryGetKnownValue(currencyCode, out var currency) && currency != null
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }
}
