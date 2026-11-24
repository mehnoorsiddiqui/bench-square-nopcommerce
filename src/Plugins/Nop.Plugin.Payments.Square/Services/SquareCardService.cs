using Nop.Core;
using Square.Models;
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

    /// <summary>
    /// Upper bound of the card pages read for one customer (Square returns at most 25 cards per page)
    /// </summary>
    private const int MaxCardPages = 20;

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
        //brand and type are enum records: switch on their wire values
        var brand = card.CardBrand?.Value switch
        {
            "VISA" => "Visa",
            "MASTERCARD" => "Mastercard",
            "AMERICAN_EXPRESS" => "American Express",
            "DISCOVER" => "Discover",
            "DISCOVER_DINERS" => "Diners Club",
            "JCB" => "JCB",
            "CHINA_UNIONPAY" => "UnionPay",
            "SQUARE_GIFT_CARD" => "Square Gift Card",
            "INTERAC" => "Interac",
            "EFTPOS" => "EFTPOS",
            _ => "Card"
        };

        var cardType = card.CardType?.Value switch
        {
            "CREDIT" => "Credit",
            "DEBIT" => "Debit",
            _ => null
        };

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

        //the customer model no longer carries the cards on file: list the customer's cards page by page
        var client = await _squareClientFactory.CreateAsync(storeId);
        var cards = new List<Card>();
        string cursor = null;
        var pages = 0;
        do
        {
            if (++pages > MaxCardPages)
                throw new NopException($"The saved cards of customer {squareCustomerId} exceed {MaxCardPages} pages and were not fully loaded");

            var response = await client.Cards.ListCards(new ListCardsOperationRequest { CustomerId = squareCustomerId, Cursor = cursor })
                ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);

            cards.AddRange(response.Cards ?? []);

            //stop when Square returns no further cursor, or the same one again
            cursor = string.IsNullOrEmpty(response.Cursor) || response.Cursor == cursor ? null : response.Cursor;
        }
        while (cursor != null);

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
