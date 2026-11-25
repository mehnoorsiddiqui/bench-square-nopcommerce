using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Resolves store codes into the values Square declares for them
/// </summary>
public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Gets the Square currency of an ISO 4217 currency code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">Square does not declare the currency</exception>
    public static Currency ToSquareCurrency(this string currencyCode)
    {
        if (!Currency.TryGetKnownValue(currencyCode?.Trim().ToUpperInvariant(), out var currency) || currency is null)
            throw new NopException($"The {currencyCode} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 country code
    /// </summary>
    /// <param name="countryCode">Two-letter country code</param>
    /// <returns>The Square country; null when the code is empty or Square does not declare it</returns>
    public static Country ToSquareCountry(this string countryCode)
    {
        return Country.TryGetKnownValue(countryCode?.Trim().ToUpperInvariant(), out var country) ? country : null;
    }

    #endregion
}
