using Nop.Core;
using SquareEnums = Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts nopCommerce codes to the Square enum values the SDK expects
/// </summary>
public static class SquareCodes
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>Square currency</returns>
    /// <exception cref="NopException">Square does not support the currency</exception>
    public static SquareEnums.Currency ToCurrency(string currencyCode)
    {
        return SquareEnums.Currency.TryGetKnownValue(currencyCode?.ToUpperInvariant(), out var currency)
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="twoLetterIsoCode">Country code</param>
    /// <returns>Square country; null when Square does not know the code</returns>
    public static SquareEnums.Country ToCountry(string twoLetterIsoCode)
    {
        return SquareEnums.Country.TryGetKnownValue(twoLetterIsoCode?.ToUpperInvariant(), out var country) ? country : null;
    }
}
