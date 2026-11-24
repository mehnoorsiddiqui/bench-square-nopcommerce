using System.Security.Cryptography;
using System.Text;
using Nop.Core;
using Square.Models;
using Square.Models.Enums;
using Square.Requests.Catalog;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// A nopCommerce product as it is published to the Square catalog (one item with one variation)
/// </summary>
/// <param name="ProductId">Product identifier</param>
/// <param name="StoreName">Name of the store the product is published from</param>
/// <param name="Sku">Product SKU</param>
/// <param name="Name">Product name</param>
/// <param name="VariationName">Name of the variation (e.g. "Regular")</param>
/// <param name="PriceAmount">Price in the smallest currency unit</param>
/// <param name="Currency">Currency code</param>
/// <param name="Published">Whether the product is published in the online store</param>
/// <param name="IsDownload">Whether the product is downloadable</param>
/// <param name="UpdatedOnUtc">When the product was last updated</param>
/// <param name="ItemId">Square item identifier, when the product was synchronized before</param>
/// <param name="VariationId">Square item variation identifier, when the product was synchronized before</param>
public record CatalogProductInfo(int ProductId, string StoreName, string Sku, string Name, string VariationName,
    long PriceAmount, string Currency, bool Published, bool IsDownload, DateTime UpdatedOnUtc,
    string ItemId = null, string VariationId = null);

/// <summary>
/// Square identifiers of a synchronized product
/// </summary>
/// <param name="ItemId">Square item identifier</param>
/// <param name="VariationId">Square item variation identifier</param>
public record CatalogSyncResult(string ItemId, string VariationId);

/// <summary>
/// Publishes nopCommerce products to the Square catalog
/// </summary>
public class SquareCatalogService
{
    #region Constants

    /// <summary>
    /// Longest idempotency key Square accepts for a catalog upsert
    /// </summary>
    private const int MaxIdempotencyKeyLength = 128;

    #endregion

    #region Fields

    private readonly ISquareClientFactory _squareClientFactory;

    #endregion

    #region Ctor

    public SquareCatalogService(ISquareClientFactory squareClientFactory)
    {
        _squareClientFactory = squareClientFactory;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Builds the idempotency key of a product update: retrying the same update reuses the key,
    /// a later update of the product gets a new one
    /// </summary>
    /// <param name="product">Product</param>
    /// <returns>Idempotency key</returns>
    public static string BuildIdempotencyKey(CatalogProductInfo product)
    {
        var key = $"nop:{product.StoreName}:{product.ProductId}:{product.Sku}:{product.VariationName}:{product.UpdatedOnUtc:yyyyMMddHHmmssfff}";

        //Square accepts at most 128 characters: a longer key is replaced by its hash, which is as stable
        if (key.Length <= MaxIdempotencyKeyLength)
            return key;

        return $"nop:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))}";
    }

    /// <summary>
    /// Creates or updates the Square item of a product
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>The asynchronous task whose result contains the Square identifiers of the product</returns>
    public async Task<CatalogSyncResult> UpsertProductAsync(CatalogProductInfo product, int storeId)
    {
        ArgumentNullException.ThrowIfNull(product);

        //new objects get temporary "#" identifiers that Square maps to real ones
        var itemId = product.ItemId ?? $"#item-{product.ProductId}";
        var variationId = product.VariationId ?? $"#variation-{product.ProductId}";

        var client = await _squareClientFactory.CreateAsync(storeId);
        var response = await client.Catalog.UpsertCatalogObject(new UpsertCatalogObjectOperationRequest
        {
            Body = new UpsertCatalogObjectRequest
            {
                IdempotencyKey = BuildIdempotencyKey(product),
                Object = new CatalogObject
                {
                    Type = CatalogObjectType.Item,
                    Id = itemId,
                    ItemData = new CatalogItem
                    {
                        Name = product.Name,
                        ProductType = CatalogItemProductType.Regular,
                        Variations =
                        [
                            new CatalogObject
                            {
                                Type = CatalogObjectType.ItemVariation,
                                Id = variationId,
                                ItemVariationData = new CatalogItemVariation
                                {
                                    ItemId = itemId,
                                    Name = product.VariationName,
                                    Sku = product.Sku,
                                    PricingType = CatalogPricingType.FixedPricing,
                                    PriceMoney = new Money { Amount = product.PriceAmount, Currency = SquareResponse.ToCurrency(product.Currency) },
                                    TrackInventory = true
                                }
                            }
                        ]
                    }
                }
            }
        }) ?? throw new NopException("No service response");

        SquareResponse.ThrowIfErrors(response.Errors);

        string resolve(string id) => response.IdMappings?.FirstOrDefault(mapping => mapping.ClientObjectId == id)?.ObjectId ?? id;

        return new CatalogSyncResult(resolve(itemId), resolve(variationId));
    }

    /// <summary>
    /// Deletes Square catalog objects (items are deleted together with their variations)
    /// </summary>
    /// <param name="objectIds">Square catalog object identifiers</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task DeleteObjectsAsync(IReadOnlyCollection<string> objectIds, int storeId)
    {
        if (!(objectIds?.Any() ?? false))
            return;

        var client = await _squareClientFactory.CreateAsync(storeId);
        var response = await client.Catalog.BatchDeleteCatalogObjects(new BatchDeleteCatalogObjectsOperationRequest
        {
            Body = new BatchDeleteCatalogObjectsRequest { ObjectIds = objectIds.ToList() }
        }) ?? throw new NopException("No service response");

        SquareResponse.ThrowIfErrors(response.Errors);
    }

    #endregion
}
