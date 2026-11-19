using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Events;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Shipping;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Publishes product and stock changes to Square when catalog/inventory synchronization is enabled
/// </summary>
public class SquareSyncEventConsumer :
    IConsumer<EntityInsertedEvent<Product>>,
    IConsumer<EntityUpdatedEvent<Product>>,
    IConsumer<EntityDeletedEvent<Product>>,
    IConsumer<EntityInsertedEvent<StockQuantityHistory>>
{
    #region Fields

    private readonly CurrencySettings _currencySettings;
    private readonly ICurrencyService _currencyService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly ILogger _logger;
    private readonly IProductService _productService;
    private readonly ISettingService _settingService;
    private readonly IShippingService _shippingService;
    private readonly IStoreContext _storeContext;
    private readonly SquareCatalogService _squareCatalogService;
    private readonly SquareInventoryService _squareInventoryService;

    #endregion

    #region Ctor

    public SquareSyncEventConsumer(CurrencySettings currencySettings,
        ICurrencyService currencyService,
        IGenericAttributeService genericAttributeService,
        ILogger logger,
        IProductService productService,
        ISettingService settingService,
        IShippingService shippingService,
        IStoreContext storeContext,
        SquareCatalogService squareCatalogService,
        SquareInventoryService squareInventoryService)
    {
        _currencySettings = currencySettings;
        _currencyService = currencyService;
        _genericAttributeService = genericAttributeService;
        _logger = logger;
        _productService = productService;
        _settingService = settingService;
        _shippingService = shippingService;
        _storeContext = storeContext;
        _squareCatalogService = squareCatalogService;
        _squareInventoryService = squareInventoryService;
    }

    #endregion

    #region Utilities

    private async Task<(int StoreId, SquarePaymentSettings Settings)> GetSettingsAsync()
    {
        var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
        return (storeId, await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeId));
    }

    private async Task UpsertProductAsync(Product product)
    {
        var (storeId, settings) = await GetSettingsAsync();
        if (!settings.SyncCatalog || product.Deleted)
            return;

        try
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId)
                ?? throw new NopException("Primary store currency cannot be loaded");

            var result = await _squareCatalogService.UpsertProductAsync(new CatalogProductInfo(
                ProductId: product.Id,
                StoreName: store.Name,
                Sku: product.Sku ?? product.Id.ToString(),
                Name: product.Name,
                VariationName: "Regular",
                PriceAmount: (long)(product.Price * 100),
                Currency: currency.CurrencyCode,
                Published: product.Published,
                IsDownload: product.IsDownload,
                UpdatedOnUtc: product.UpdatedOnUtc,
                ItemId: await _genericAttributeService.GetAttributeAsync<string>(product, SquarePaymentDefaults.CatalogItemIdAttribute),
                VariationId: await _genericAttributeService.GetAttributeAsync<string>(product, SquarePaymentDefaults.CatalogVariationIdAttribute)), storeId);

            await _genericAttributeService.SaveAttributeAsync(product, SquarePaymentDefaults.CatalogItemIdAttribute, result.ItemId);
            await _genericAttributeService.SaveAttributeAsync(product, SquarePaymentDefaults.CatalogVariationIdAttribute, result.VariationId);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync($"Square catalog sync error: product #{product.Id} was not published. {exception.Message}", exception);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Publishes a new product
    /// </summary>
    public Task HandleEventAsync(EntityInsertedEvent<Product> eventMessage) => UpsertProductAsync(eventMessage.Entity);

    /// <summary>
    /// Publishes a changed product (a soft-deleted product is removed from Square)
    /// </summary>
    public async Task HandleEventAsync(EntityUpdatedEvent<Product> eventMessage)
    {
        if (eventMessage.Entity.Deleted)
        {
            await HandleEventAsync(new EntityDeletedEvent<Product>(eventMessage.Entity));
            return;
        }

        await UpsertProductAsync(eventMessage.Entity);
    }

    /// <summary>
    /// Removes a deleted product from Square
    /// </summary>
    public async Task HandleEventAsync(EntityDeletedEvent<Product> eventMessage)
    {
        var (storeId, settings) = await GetSettingsAsync();
        if (!settings.SyncCatalog)
            return;

        var product = eventMessage.Entity;
        var itemId = await _genericAttributeService.GetAttributeAsync<string>(product, SquarePaymentDefaults.CatalogItemIdAttribute);
        if (string.IsNullOrEmpty(itemId))
            return;

        try
        {
            await _squareCatalogService.DeleteObjectsAsync([itemId], storeId);
            await _genericAttributeService.SaveAttributeAsync<string>(product, SquarePaymentDefaults.CatalogItemIdAttribute, null);
            await _genericAttributeService.SaveAttributeAsync<string>(product, SquarePaymentDefaults.CatalogVariationIdAttribute, null);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync($"Square catalog sync error: product #{product.Id} was not removed. {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Sends a stock change to Square
    /// </summary>
    public async Task HandleEventAsync(EntityInsertedEvent<StockQuantityHistory> eventMessage)
    {
        var (storeId, settings) = await GetSettingsAsync();
        var history = eventMessage.Entity;
        if (!settings.SyncInventory || history.QuantityAdjustment == 0)
            return;

        var product = await _productService.GetProductByIdAsync(history.ProductId);
        var variationId = product == null ? null
            : await _genericAttributeService.GetAttributeAsync<string>(product, SquarePaymentDefaults.CatalogVariationIdAttribute);
        if (string.IsNullOrEmpty(variationId))
            return;

        //a warehouse may be mapped to its own Square location; the store's location is the default
        var warehouse = history.WarehouseId.HasValue ? await _shippingService.GetWarehouseByIdAsync(history.WarehouseId.Value) : null;
        var locationId = (warehouse == null ? null
            : await _genericAttributeService.GetAttributeAsync<string>(warehouse, SquarePaymentDefaults.WarehouseLocationIdAttribute)) ?? settings.LocationId;

        try
        {
            await _squareInventoryService.AdjustStockAsync(variationId, locationId, history.QuantityAdjustment, history.CreatedOnUtc, storeId);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync($"Square inventory sync error: stock change of product #{product.Id} was not sent. {exception.Message}", exception);
        }
    }

    #endregion
}
