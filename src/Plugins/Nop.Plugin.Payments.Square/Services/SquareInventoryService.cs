using System.Globalization;
using Nop.Core;
using Square.Models;
using Square.Models.Enums;
using Square.Requests.Inventory;

namespace Nop.Plugin.Payments.Square.Services;

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

    private async Task ChangeInventoryAsync(InventoryChange change, int storeId)
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
                //the stock stays at one location: it is tracked there both before and after the adjustment
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
