using Nop.Core;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Extensions;

public static class SquareEnumExtensions
{
    #region Methods

    /// <summary>
    /// Gets the Square currency of an ISO 4217 code
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>The Square currency</returns>
    /// <exception cref="NopException">The code is not a currency Square declares</exception>
    public static Currency ToSquareCurrency(this string code)
    {
        return Currency.TryGetKnownValue(code, out var currency) && currency != null
            ? currency
            : throw new NopException($"The {code} currency is not supported by the Square");
    }

    /// <summary>
    /// Gets the Square country of an ISO 3166-1 alpha-2 code
    /// </summary>
    /// <param name="code">Country code</param>
    /// <returns>The Square country; null when the code is empty or not a country Square declares</returns>
    public static Country ToSquareCountry(this string code)
    {
        return Country.TryGetKnownValue(code, out var country) ? country : null;
    }

    #endregion
}
