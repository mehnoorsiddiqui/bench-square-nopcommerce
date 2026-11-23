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

        //the customer profile no longer lists its cards: read them from the Cards API, which returns them page by page
        var client = await _squareClientFactory.CreateAsync(storeId);
        var cards = new List<Card>();
        string cursor = null;
        do
        {
            var response = await client.Cards.ListCards(new ListCardsOperationRequest { CustomerId = squareCustomerId, Cursor = cursor })
                ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);

            cards.AddRange(response.Cards ?? []);
            cursor = response.Cursor;
        }
        while (!string.IsNullOrEmpty(cursor));

        return cards
            .Where(card => card.Enabled != false)
            .Select(ToSavedCard)
            .ToList();
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
