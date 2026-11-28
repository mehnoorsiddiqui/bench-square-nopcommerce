using System.Globalization;
using Nop.Core;
using Square.Models;
using Square.Models.Enums;
using Square.Requests.Inventory;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Result of moving stock between two Square locations
/// </summary>
/// <param name="Id">Square identifier of the stock movement</param>
/// <param name="Completed">Whether the stock has arrived at the destination location</param>
public record StockMoveResult(string Id, bool Completed);

/// <summary>
/// Keeps Square inventory in step with nopCommerce stock
/// </summary>
public class SquareInventoryService
{
    #region Fields

    private readonly ISquareClientFactory _squareClientFactory;

    #endregion

    #region Ctor

    public SquareInventoryService(ISquareClientFactory squareClientFactory)
    {
        _squareClientFactory = squareClientFactory;
    }

    #endregion

    #region Utilities

    private async Task<BatchChangeInventoryResponse> ChangeInventoryAsync(InventoryChange change, int storeId)
    {
        var client = await _squareClientFactory.CreateAsync(storeId);
        var response = await client.Inventory.BatchChangeInventory(new BatchChangeInventoryOperationRequest
        {
            Body = new BatchChangeInventoryRequest
            {
                IdempotencyKey = Guid.NewGuid().ToString(),
                Changes = [change],
                IgnoreUnchangedCounts = true
            }
        }) ?? throw new NopException("No service response");

        SquareResponse.ThrowIfErrors(response.Errors);

        return response;
    }

    private static string FormatQuantity(int quantity) => quantity.ToString(CultureInfo.InvariantCulture);

    private static string FormatTime(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    #endregion

    #region Methods

    /// <summary>
    /// Records a stock change at one location: a positive quantity was received, a negative one was sold
    /// </summary>
    /// <param name="catalogObjectId">Square item variation identifier</param>
    /// <param name="locationId">Square location identifier</param>
    /// <param name="quantityDelta">Change of the stock quantity</param>
    /// <param name="occurredAtUtc">When the change happened</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task AdjustStockAsync(string catalogObjectId, string locationId, int quantityDelta, DateTime occurredAtUtc, int storeId)
    {
        if (quantityDelta == 0)
            return;

        await ChangeInventoryAsync(new InventoryChange
        {
            Type = InventoryChangeType.Adjustment,
            Adjustment = new InventoryAdjustment
            {
                CatalogObjectId = catalogObjectId,
                FromLocationId = locationId,
                ToLocationId = locationId,
                FromState = quantityDelta > 0 ? InventoryState.None : InventoryState.InStock,
                ToState = quantityDelta > 0 ? InventoryState.InStock : InventoryState.Sold,
                Quantity = FormatQuantity(Math.Abs(quantityDelta)),
                OccurredAt = FormatTime(occurredAtUtc)
            }
        }, storeId);
    }

    /// <summary>
    /// Moves stock from one location to another (a warehouse transfer in nopCommerce)
    /// </summary>
    /// <param name="catalogObjectId">Square item variation identifier</param>
    /// <param name="fromLocationId">Square location the stock leaves</param>
    /// <param name="toLocationId">Square location the stock arrives at</param>
    /// <param name="quantity">Quantity to move</param>
    /// <param name="occurredAtUtc">When the move happened</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>The asynchronous task whose result contains the result of the move</returns>
    public async Task<StockMoveResult> MoveStockAsync(string catalogObjectId, string fromLocationId, string toLocationId,
        int quantity, DateTime occurredAtUtc, int storeId)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "The quantity to move must be positive");

        var response = await ChangeInventoryAsync(new InventoryChange
        {
            //transfers are adjustments that name both locations
            Type = InventoryChangeType.Adjustment,
            Adjustment = new InventoryAdjustment
            {
                CatalogObjectId = catalogObjectId,
                FromLocationId = fromLocationId,
                ToLocationId = toLocationId,
                FromState = InventoryState.InStock,
                ToState = InventoryState.InStock,
                ReasonId = new InventoryAdjustmentReasonId { Type = InventoryAdjustmentReasonIdType.Transfer },
                Quantity = FormatQuantity(quantity),
                OccurredAt = FormatTime(occurredAtUtc)
            }
        }, storeId);

        var transferId = response.Changes?.FirstOrDefault(change => change.Adjustment?.Id != null)?.Adjustment?.Id
            ?? throw new NopException("Square did not report the transfer");

        //read the transfer back: it is complete once the stock is in stock at the destination
        var client = await _squareClientFactory.CreateAsync(storeId);
        var transferResponse = await client.Inventory.RetrieveInventoryAdjustment(new RetrieveInventoryAdjustmentRequest { AdjustmentId = transferId })
            ?? throw new NopException("No service response");

        SquareResponse.ThrowIfErrors(transferResponse.Errors);

        var state = transferResponse.Adjustment?.ToState;
        bool completed;
        if (state == InventoryState.InStock)
            completed = true;
        else if (state == InventoryState.InTransit || state == InventoryState.InTransitTo)
            completed = false;
        else
            throw new NopException($"Unexpected state of transfer {transferId}: {state?.Value}");

        return new StockMoveResult(transferId, completed);
    }

    /// <summary>
    /// Gets the quantity in stock at a location
    /// </summary>
    /// <param name="catalogObjectId">Square item variation identifier</param>
    /// <param name="locationId">Square location identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>The asynchronous task whose result contains the quantity in stock</returns>
    public async Task<decimal> GetInStockQuantityAsync(string catalogObjectId, string locationId, int storeId)
    {
        var client = await _squareClientFactory.CreateAsync(storeId);
        var response = await client.Inventory.BatchRetrieveInventoryCounts(new BatchRetrieveInventoryCountsOperationRequest
        {
            Body = new BatchRetrieveInventoryCountsRequest
            {
                CatalogObjectIds = [catalogObjectId],
                LocationIds = [locationId],
                States = [InventoryState.InStock]
            }
        }) ?? throw new NopException("No service response");

        SquareResponse.ThrowIfErrors(response.Errors);

        return (response.Counts ?? [])
            .Where(count => count.State == InventoryState.InStock)
            .Sum(count => decimal.Parse(count.Quantity ?? "0", CultureInfo.InvariantCulture));
    }

    #endregion
}
