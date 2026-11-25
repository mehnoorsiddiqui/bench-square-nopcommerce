using System.Net;
using Moq;
using Nop.Core;
using Nop.Plugin.Payments.Square.Services;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using NUnit.Framework;
using Square.Models;
using Square.Models.Enums;

namespace Nop.Plugin.Payments.Square.Tests;

[TestFixture]
public class SquareCardServiceTests
{
    [Test]
    public async Task SavedCards_ShowsEnabledCardsWithBrand()
    {
        var square = new StubSquare().Respond("GET /v2/cards", new ListCardsResponse
        {
            Cards =
            [
                new Card { Id = "ccof:1", CardBrand = CardBrand.Visa, CardType = CardType.Credit, Last4 = "1111", ExpMonth = 12, ExpYear = 2030, Enabled = true },
                new Card { Id = "ccof:2", CardBrand = CardBrand.Mastercard, CardType = CardType.Debit, Last4 = "4444", ExpMonth = 1, ExpYear = 2029, Enabled = false }
            ]
        });

        var cards = await new SquareCardService(square).GetSavedCardsAsync("C1", 1);

        Assert.That(cards, Has.Count.EqualTo(1));
        Assert.That(cards[0].Brand, Is.EqualTo("Visa"));
        Assert.That(cards[0].CardType, Is.EqualTo("Credit"));
        Assert.That(cards[0].Last4, Is.EqualTo("1111"));
    }
}

[TestFixture]
public class LegacyTransactionServiceTests
{
    [Test]
    public async Task RefundableBalance_SubtractsApprovedRefunds()
    {
        var square = new StubSquare().Respond("GET /v2/locations/L1/transactions/T1", new RetrieveTransactionResponse
        {
            Transaction = new Transaction
            {
                Id = "T1",
                LocationId = "L1",
                Tenders = [new Tender { Id = "TND1", Type = TenderType.Card, AmountMoney = new Money { Amount = 10000, Currency = Currency.Usd } }],
                Refunds =
                [
                    new Refund { Id = "R1", LocationId = "L1", TenderId = "TND1", Reason = "damaged", Status = RefundStatus.Approved, AmountMoney = new Money { Amount = 2500, Currency = Currency.Usd } },
                    new Refund { Id = "R2", LocationId = "L1", TenderId = "TND1", Reason = "duplicate", Status = RefundStatus.Rejected, AmountMoney = new Money { Amount = 1000, Currency = Currency.Usd } }
                ]
            }
        });

        var balances = await new LegacyTransactionService(square).GetRefundableBalancesAsync(new LegacyTransactionReference("L1", "T1"), 1);

        Assert.That(balances.Single().Refundable, Is.EqualTo(7500));
    }

    [Test]
    public void Reference_RoundTrips()
    {
        Assert.That(LegacyTransactionReference.TryParse("legacy:L1:T1", out var reference), Is.True);
        Assert.That(reference, Is.EqualTo(new LegacyTransactionReference("L1", "T1")));
        Assert.That(LegacyTransactionReference.TryParse("pay_123", out _), Is.False);
    }
}

[TestFixture]
public class SquareInventoryServiceTests
{
    [Test]
    public async Task AdjustStock_SendsAnAdjustmentAtTheLocation()
    {
        var square = new StubSquare().Respond("POST /v2/inventory/changes/batch-create", new BatchChangeInventoryResponse());

        await new SquareInventoryService(square).AdjustStockAsync("VAR1", "L1", 5, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1);

        var change = square.RequestBody<BatchChangeInventoryRequest>("POST /v2/inventory/changes/batch-create").Changes!.Single();
        Assert.That(change.Type, Is.EqualTo(InventoryChangeType.Adjustment));
        Assert.That(change.Adjustment!.LocationId, Is.EqualTo("L1"));
        Assert.That(change.Adjustment.FromState, Is.EqualTo(InventoryState.None));
        Assert.That(change.Adjustment.ToState, Is.EqualTo(InventoryState.InStock));
        Assert.That(change.Adjustment.Quantity, Is.EqualTo("5"));
    }
}

[TestFixture]
public class SquareCatalogServiceTests
{
    [Test]
    public async Task UpsertProduct_PublishesTheItemOnline()
    {
        var square = new StubSquare().Respond("POST /v2/catalog/object", new UpsertCatalogObjectResponse
        {
            IdMappings =
            [
                new CatalogIdMapping { ClientObjectId = "#item-5", ObjectId = "ITEM5" },
                new CatalogIdMapping { ClientObjectId = "#variation-5", ObjectId = "VAR5" }
            ]
        });

        var result = await new SquareCatalogService(square).UpsertProductAsync(new CatalogProductInfo(5, "Store", "SKU5", "Lamp", "Regular",
            1999, "USD", Published: true, IsDownload: false, UpdatedOnUtc: new DateTime(2026, 1, 1)), 1);

        Assert.That(result, Is.EqualTo(new CatalogSyncResult("ITEM5", "VAR5")));
        var item = square.RequestBody<UpsertCatalogObjectRequest>("POST /v2/catalog/object").Object;
        Assert.That(item.ItemData!.ProductType, Is.EqualTo(CatalogItemProductType.Regular));
        Assert.That(item.ItemData.Variations!.Single().ItemVariationData!.PricingType, Is.EqualTo(CatalogPricingType.FixedPricing));
    }

    [Test]
    public void IdempotencyKey_StaysWithinSquareLimit_AndIsStable()
    {
        var product = new CatalogProductInfo(5, new string('S', 80), new string('K', 80), "Lamp", new string('V', 80),
            1999, "USD", Published: true, IsDownload: false, UpdatedOnUtc: new DateTime(2026, 1, 1));

        var key = SquareCatalogService.BuildIdempotencyKey(product);

        Assert.That(key.Length, Is.InRange(1, 128));
        Assert.That(SquareCatalogService.BuildIdempotencyKey(product), Is.EqualTo(key));
        Assert.That(SquareCatalogService.BuildIdempotencyKey(product with { UpdatedOnUtc = new DateTime(2026, 1, 2) }), Is.Not.EqualTo(key));
    }
}

[TestFixture]
public class SquarePaymentManagerTests
{
    [Test]
    public async Task CreatePayment_DeclinedCard_ReturnsPlainMessage()
    {
        var square = new StubSquare().Respond("POST /v2/payments", new CreatePaymentResponse
        {
            Errors = [new Error { Category = ErrorCategory.PaymentMethodError, Code = ErrorCode.CardDeclined, Detail = "Authorization error" }]
        }, HttpStatusCode.PaymentRequired);
        var manager = new SquarePaymentManager(square, new Mock<ILogger>().Object, new Mock<ISettingService>().Object, new Mock<IWorkContext>().Object, null);

        var (payment, error) = await manager.CreatePaymentAsync(new CreatePaymentRequest
        {
            SourceId = "cnon:card",
            IdempotencyKey = "key",
            AmountMoney = new Money { Amount = 100, Currency = Currency.Usd }
        }, 1);

        Assert.That(payment, Is.Null);
        Assert.That(error, Is.EqualTo("The card was declined. Please use a different card."));
    }
}
