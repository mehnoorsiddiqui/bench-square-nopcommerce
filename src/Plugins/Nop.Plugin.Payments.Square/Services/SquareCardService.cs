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
    /// Maximum number of card pages read for one customer (Square returns at most 25 cards per page)
    /// </summary>
    private const int MaxCardPages = 40;

    private static readonly Dictionary<CardBrand, string> BrandNames = new()
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

    private static SavedCardInfo ToSavedCard(Card card)
    {
        //card brands and types are SDK enums (not constants), so they are looked up rather than matched as patterns
        var brand = card.CardBrand is { } cardBrand && BrandNames.TryGetValue(cardBrand, out var brandName) ? brandName : "Card";

        string cardType = null;
        if (card.CardType == CardType.Credit)
            cardType = "Credit";
        else if (card.CardType == CardType.Debit)
            cardType = "Debit";

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

        //the customer's cards are listed by the Cards API, a page (at most 25 cards) at a time
        var client = await _squareClientFactory.CreateAsync(storeId);
        var cards = new List<Card>();
        string cursor = null;
        for (var page = 0; ; page++)
        {
            if (page >= MaxCardPages)
                throw new NopException($"Square returned more than {MaxCardPages} pages of cards for customer {squareCustomerId}");

            var response = await client.Cards.ListCards(new ListCardsOperationRequest { CustomerId = squareCustomerId, Cursor = cursor })
                ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);

            cards.AddRange(response.Cards ?? []);

            if (string.IsNullOrEmpty(response.Cursor))
                break;
            if (response.Cursor == cursor)
                throw new NopException("Square did not advance the card list cursor");

            cursor = response.Cursor;
        }

        return cards
            .Where(card => card != null && card.Enabled != false && (card.CustomerId == null || card.CustomerId == squareCustomerId))
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
