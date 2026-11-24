using System.Net;
using System.Text.Json;
using Nop.Core;
using Square.Core.ErrorResponse;
using Square.Core.Exceptions;
using Square.Models;
using Square.Models.Enums;
using Square.Requests.CustomerCustomAttributes;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Links Square customers to nopCommerce customers through a hidden customer custom attribute
/// </summary>
public class SquareCustomerLinkService
{
    #region Constants

    /// <summary>
    /// Key of the custom attribute holding the nopCommerce customer GUID
    /// </summary>
    public const string AttributeKey = "nop-customer-guid";

    #endregion

    #region Fields

    private readonly ISquareClientFactory _squareClientFactory;

    #endregion

    #region Ctor

    public SquareCustomerLinkService(ISquareClientFactory squareClientFactory)
    {
        _squareClientFactory = squareClientFactory;
    }

    #endregion

    #region Utilities

    private static bool IsNotFound(ApiException<RawError> exception) => exception.Error.StatusCode == HttpStatusCode.NotFound;

    /// <summary>
    /// Creates the attribute definition the first time a customer is linked
    /// </summary>
    private static async Task EnsureDefinitionAsync(global::Square.SquareClient client)
    {
        try
        {
            var response = await client.CustomerCustomAttributes.RetrieveCustomerCustomAttributeDefinition(
                new RetrieveCustomerCustomAttributeDefinitionOperationRequest { Key = AttributeKey });
            SquareResponse.ThrowIfErrors(response?.Errors);
        }
        catch (ApiException<RawError> exception) when (IsNotFound(exception))
        {
            var response = await client.CustomerCustomAttributes.CreateCustomerCustomAttributeDefinition(new CreateCustomerCustomAttributeDefinitionOperationRequest
            {
                Body = new CreateCustomerCustomAttributeDefinitionRequest
                {
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    CustomAttributeDefinition = new CustomAttributeDefinition
                    {
                        Key = AttributeKey,
                        Name = "nopCommerce customer",
                        Description = "GUID of the nopCommerce customer this Square customer belongs to",
                        Schema = new Dictionary<string, string>
                        {
                            ["$ref"] = "https://developer-production-s.squarecdn.com/schemas/v1/common.json#squareup.common.String"
                        },
                        Visibility = CustomAttributeDefinitionVisibility.VisibilityHidden
                    }
                }
            }) ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Stores the nopCommerce customer GUID on the Square customer
    /// </summary>
    /// <param name="squareCustomerId">Square customer identifier</param>
    /// <param name="customerGuid">nopCommerce customer GUID</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task LinkAsync(string squareCustomerId, Guid customerGuid, int storeId)
    {
        ArgumentException.ThrowIfNullOrEmpty(squareCustomerId);

        var client = await _squareClientFactory.CreateAsync(storeId);
        await EnsureDefinitionAsync(client);

        var response = await client.CustomerCustomAttributes.UpsertCustomerCustomAttribute(new UpsertCustomerCustomAttributeOperationRequest
        {
            CustomerId = squareCustomerId,
            Key = AttributeKey,
            Body = new UpsertCustomerCustomAttributeRequest
            {
                IdempotencyKey = Guid.NewGuid().ToString(),
                CustomAttribute = new CustomAttribute { Value = customerGuid.ToString() }
            }
        }) ?? throw new NopException("No service response");

        SquareResponse.ThrowIfErrors(response.Errors);
    }

    /// <summary>
    /// Gets the nopCommerce customer GUID stored on the Square customer
    /// </summary>
    /// <param name="squareCustomerId">Square customer identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>The asynchronous task whose result contains the customer GUID; null when the customer is not linked</returns>
    public async Task<Guid?> GetLinkedCustomerGuidAsync(string squareCustomerId, int storeId)
    {
        if (string.IsNullOrEmpty(squareCustomerId))
            return null;

        var client = await _squareClientFactory.CreateAsync(storeId);
        try
        {
            var response = await client.CustomerCustomAttributes.RetrieveCustomerCustomAttribute(new RetrieveCustomerCustomAttributeOperationRequest
            {
                CustomerId = squareCustomerId,
                Key = AttributeKey
            }) ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);

            var value = response.CustomAttribute?.Value switch
            {
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                string text => text,
                _ => null
            };

            return Guid.TryParse(value, out var guid) ? guid : null;
        }
        catch (ApiException<RawError> exception) when (IsNotFound(exception))
        {
            return null;
        }
    }

    #endregion
}
