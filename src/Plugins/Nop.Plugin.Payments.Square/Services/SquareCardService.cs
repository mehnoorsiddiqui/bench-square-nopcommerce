using Nop.Core;
using Square.Models;
using Square.Models.Enums;
using Square.Requests.Cards;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// A card a customer saved on file, as shown in the store
/// </summary>
/// <param name="Id">Square card identifier</param>
/// <param name="Brand">Display name of the card brand</param>
/// <param name="CardType">Display name of the card type (credit, debit); null when Square does not know it</param>
/// <param name="Last4">Last four digits of the card number</param>
/// <param name="ExpMonth">Expiration month</param>
/// <param name="ExpYear">Expiration year</param>
public record SavedCardInfo(string Id, string Brand, string CardType, string Last4, long? ExpMonth, long? ExpYear);

/// <summary>
/// Reads and removes the cards customers saved on file
/// </summary>
public class SquareCardService
{
    #region Constants

    /// <summary>
    /// The most pages of cards read for one customer
    /// </summary>
    private const int MaxPages = 20;

    #endregion

    #region Fields

    private readonly ISquareClientFactory _squareClientFactory;

    #endregion

    #region Ctor

    public SquareCardService(ISquareClientFactory squareClientFactory)
    {
        _squareClientFactory = squareClientFactory;
    }

    #endregion

    #region Utilities

    private static readonly IReadOnlyDictionary<CardBrand, string> BrandNames = new Dictionary<CardBrand, string>
    {
        [CardBrand.Visa] = "Visa",
        [CardBrand.Mastercard] = "Mastercard",
        [CardBrand.AmericanExpress] = "American Express",
        [CardBrand.Discover] = "Discover",
        [CardBrand.DiscoverDiners] = "Diners Club",
        [CardBrand.Jcb] = "JCB",
        [CardBrand.ChinaUnionpay] = "UnionPay",
        [CardBrand.SquareGiftCard] = "Square Gift Card",
        [CardBrand.Interac] = "Interac",
        [CardBrand.Eftpos] = "EFTPOS"
    };

    private static SavedCardInfo ToSavedCard(Card card)
    {
        //a brand the plugin has no name for (including one Square added later) is shown as a plain card
        var brand = card.CardBrand != null && BrandNames.TryGetValue(card.CardBrand, out var brandName) ? brandName : "Card";

        var cardType = card.CardType == CardType.Credit ? "Credit"
            : card.CardType == CardType.Debit ? "Debit"
            : null;

        return new SavedCardInfo(card.Id, brand, cardType, card.Last4, card.ExpMonth, card.ExpYear);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the enabled cards a customer saved on file
    /// </summary>
    /// <param name="squareCustomerId">Square customer identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>The asynchronous task whose result contains the saved cards</returns>
    public async Task<IList<SavedCardInfo>> GetSavedCardsAsync(string squareCustomerId, int storeId)
    {
        if (string.IsNullOrEmpty(squareCustomerId))
            return new List<SavedCardInfo>();

        var client = await _squareClientFactory.CreateAsync(storeId);

        //Square lists the cards a page at a time (disabled cards are left out unless asked for);
        //a list cut short would hide cards from the customer, so a result that does not end within the page cap is an error
        var cards = new List<SavedCardInfo>();
        string cursor = null;
        for (var page = 0; page < MaxPages; page++)
        {
            var response = await client.Cards.ListCards(new ListCardsOperationRequest { CustomerId = squareCustomerId, Cursor = cursor })
                ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);

            cards.AddRange((response.Cards ?? [])
                .Where(card => card.Enabled != false)
                .Select(ToSavedCard));

            if (string.IsNullOrEmpty(response.Cursor))
                return cards;

            //a cursor that does not move would repeat the same page for ever
            if (response.Cursor == cursor)
                break;

            cursor = response.Cursor;
        }

        throw new NopException($"The cards of Square customer {squareCustomerId} could not be listed completely");
    }

    /// <summary>
    /// Disables (removes) a card the customer saved on file
    /// </summary>
    /// <param name="squareCustomerId">Square customer identifier</param>
    /// <param name="cardId">Square card identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task DisableCardAsync(string squareCustomerId, string cardId, int storeId)
    {
        //a customer may only remove their own cards
        var cards = await GetSavedCardsAsync(squareCustomerId, storeId);
        if (!cards.Any(card => card.Id == cardId))
            throw new NopException("The card is not saved for this customer");

        var client = await _squareClientFactory.CreateAsync(storeId);
        var response = await client.Cards.DisableCard(new DisableCardRequest { CardId = cardId })
            ?? throw new NopException("No service response");

        SquareResponse.ThrowIfErrors(response.Errors);
    }

    #endregion
}
