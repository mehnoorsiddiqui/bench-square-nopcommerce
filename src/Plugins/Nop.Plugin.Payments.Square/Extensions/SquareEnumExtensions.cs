using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts plain codes to the enums the Square SDK declares
/// </summary>
public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Gets the Square currency for an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The currency; null when the code is empty</returns>
    /// <exception cref="NopException">The currency is not one Square declares</exception>
    public static Currency ToSquareCurrency(this string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        if (!Currency.TryGetKnownValue(code, out var currency))
            throw new NopException($"The {code} currency is not supported by the Square");

        return currency;
    }

    /// <summary>
    /// Gets the Square country for an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>The country; null when the code is empty or not one Square declares</returns>
    public static Country ToSquareCountry(this string code)
    {
        return Country.TryGetKnownValue(code, out var country) ? country : null;
    }

    #endregion
}
