using Nop.Core;
using Square.Models;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Helpers for reading Square responses and building Square requests
/// </summary>
public static class SquareResponse
{
    /// <summary>
    /// Resolves an ISO 4217 currency code to the currency the Square SDK declares
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The SDK does not declare the currency, so it cannot be sent</exception>
    public static Currency ToSquareCurrency(string currencyCode)
    {
        //the lookup is exact and case-sensitive, while the store accepts a code in any case
        if (Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {currencyCode} currency is not supported by the Square");
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
