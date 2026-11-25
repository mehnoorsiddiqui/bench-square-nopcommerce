using Nop.Core;
using Square.Models;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Helpers for reading Square responses
/// </summary>
public static class SquareResponse
{
    /// <summary>
    /// Resolves an ISO 4217 currency code to the SDK's currency enum
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The currency</returns>
    public static global::Square.Models.Enums.Currency ToCurrency(string code)
    {
        return global::Square.Models.Enums.Currency.TryGetKnownValue(code?.ToUpperInvariant(), out var currency)
            ? currency
            : throw new NopException($"The {code} currency is not supported by the Square SDK");
    }

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
}
