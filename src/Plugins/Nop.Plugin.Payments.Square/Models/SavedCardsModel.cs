using Nop.Plugin.Payments.Square.Services;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Square.Models;

/// <summary>
/// Represents the customer's saved cards page model
/// </summary>
public record SavedCardsModel : BaseNopModel
{
    /// <summary>
    /// Gets or sets the saved cards
    /// </summary>
    public IList<SavedCardInfo> Cards { get; set; } = new List<SavedCardInfo>();

    /// <summary>
    /// Gets or sets the error shown when the cards cannot be loaded
    /// </summary>
    public string Error { get; set; }
}
