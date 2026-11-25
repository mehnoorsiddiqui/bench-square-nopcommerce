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
    /// Converts an ISO 4217 currency code to the SDK's currency value
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The SDK currency</returns>
    public static Currency ToCurrency(string code)
    {
        if (Currency.TryGetKnownValue(code?.ToUpperInvariant(), out var currency))
            return currency;

        throw new NopException($"The {code} currency is not supported by the Square SDK");
    }

    /// <summary>
    /// Converts an ISO 3166 country code to the SDK's country value
    /// </summary>
    /// <param name="code">Two-letter country code</param>
    /// <returns>The SDK country; null when the code is not one the SDK declares</returns>
    public static Country ToCountry(string code)
    {
        return Country.TryGetKnownValue(code?.ToUpperInvariant(), out var country) ? country : null;
    }
}
