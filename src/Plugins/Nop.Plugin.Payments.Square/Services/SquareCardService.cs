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

    private static SavedCardInfo ToSavedCard(Card card)
    {
        var cardBrand = card.CardBrand;
        var brand = cardBrand switch
        {
            _ when cardBrand == CardBrand.Visa => "Visa",
            _ when cardBrand == CardBrand.Mastercard => "Mastercard",
            _ when cardBrand == CardBrand.AmericanExpress => "American Express",
            _ when cardBrand == CardBrand.Discover => "Discover",
            _ when cardBrand == CardBrand.DiscoverDiners => "Diners Club",
            _ when cardBrand == CardBrand.Jcb => "JCB",
            _ when cardBrand == CardBrand.ChinaUnionpay => "UnionPay",
            _ when cardBrand == CardBrand.SquareGiftCard => "Square Gift Card",
            _ when cardBrand == CardBrand.Interac => "Interac",
            _ when cardBrand == CardBrand.Eftpos => "EFTPOS",
            _ => "Card"
        };

        var type = card.CardType;
        var cardType = type switch
        {
            _ when type == CardType.Credit => "Credit",
            _ when type == CardType.Debit => "Debit",
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

        var client = await _squareClientFactory.CreateAsync(storeId);

        //the customer's cards are listed page by page (a page holds at most 25 cards); read every page,
        //so the result is complete (a failed page throws rather than returning part of the cards)
        var cards = new List<Card>();
        string cursor = null;
        do
        {
            var response = await client.Cards.ListCards(new ListCardsOperationRequest { CustomerId = squareCustomerId, Cursor = cursor })
                ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);

            cards.AddRange(response.Cards ?? []);

            //guard against a cursor that does not advance
            if (!string.IsNullOrEmpty(response.Cursor) && response.Cursor == cursor)
                throw new NopException("Square returned the same page of cards twice");

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
