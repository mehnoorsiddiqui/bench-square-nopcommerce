using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

/// <summary>
/// Converts the plain codes the store works with into Square SDK enums, which have no public factory
/// </summary>
public static class SquareEnumHelper
{
    #region Methods

    /// <summary>
    /// Gets the Square currency for an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The Square currency; null when the code is empty</returns>
    /// <exception cref="NopException">The SDK does not declare the currency</exception>
    public static Currency ToSquareCurrency(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        return Currency.TryGetKnownValue(code.ToUpperInvariant(), out var currency)
            ? currency
            : throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country for an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>The Square country; null when the code is empty or not declared by the SDK</returns>
    public static Country ToSquareCountry(string code)
    {
        return Country.TryGetKnownValue(code?.ToUpperInvariant(), out var country) ? country : null;
    }

    #endregion
}
