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
    /// Most pages of cards read for one customer
    /// </summary>
    private const int MaxCardPages = 20;

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
        //brand and type are open enums: a value the SDK does not declare compares unequal to every constant
        var cardBrand = card.CardBrand;
        var brand = cardBrand == CardBrand.Visa ? "Visa"
            : cardBrand == CardBrand.Mastercard ? "Mastercard"
            : cardBrand == CardBrand.AmericanExpress ? "American Express"
            : cardBrand == CardBrand.Discover ? "Discover"
            : cardBrand == CardBrand.DiscoverDiners ? "Diners Club"
            : cardBrand == CardBrand.Jcb ? "JCB"
            : cardBrand == CardBrand.ChinaUnionpay ? "UnionPay"
            : cardBrand == CardBrand.SquareGiftCard ? "Square Gift Card"
            : cardBrand == CardBrand.Interac ? "Interac"
            : cardBrand == CardBrand.Eftpos ? "EFTPOS"
            : "Card";

        var type = card.CardType;
        var cardType = type == CardType.Credit ? "Credit"
            : type == CardType.Debit ? "Debit"
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

        //the customer no longer lists its cards: they are read from the cards of the customer, page by page
        var cards = new List<Card>();
        string cursor = null;
        for (var page = 0; page < MaxCardPages; page++)
        {
            var response = await client.Cards.ListCards(new ListCardsOperationRequest { CustomerId = squareCustomerId, Cursor = cursor })
                ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);

            cards.AddRange(response.Cards ?? []);

            cursor = response.Cursor;
            if (string.IsNullOrEmpty(cursor))
                return cards
                    .Where(card => card.Enabled != false)
                    .Select(ToSavedCard)
                    .ToList();
        }

        //a partial list would hide cards the customer owns (and let a removal be refused wrongly), so it is not returned
        throw new NopException($"The cards of customer {squareCustomerId} could not be read in {MaxCardPages} pages");
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
