using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Converts the plain codes the store works with to the enum types of the Square SDK
/// </summary>
public static class SquareValues
{
    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency; null when the code is empty</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency ToCurrency(string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode))
            return null;

        return Currency.TryGetKnownValue(currencyCode, out var currency)
            ? currency
            : throw new NopException($"The {currencyCode} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="countryCode">Country code</param>
    /// <returns>The Square country; null when the code is empty or Square does not declare the country</returns>
    public static Country ToCountry(string countryCode)
    {
        return !string.IsNullOrEmpty(countryCode) && Country.TryGetKnownValue(countryCode, out var country) ? country : null;
    }
}
