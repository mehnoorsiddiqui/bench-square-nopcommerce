using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Square.Core.ErrorResponse;
using Square.Core.Exceptions;
using Square.Models;
using Square.Models.Enums;
using Square.Requests.Cards;
using Square.Requests.Customers;
using Square.Requests.Locations;
using Square.Requests.Payments;
using Square.Requests.Refunds;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Represents the Square payment manager
/// </summary>
public class SquarePaymentManager
{
    #region Fields

    private readonly ISquareClientFactory _squareClientFactory;
    private readonly ILogger _logger;
    private readonly ISettingService _settingService;
    private readonly IWorkContext _workContext;
    private readonly SquareAuthorizationHttpClient _squareAuthorizationHttpClient;

    #endregion

    #region Ctor

    public SquarePaymentManager(ISquareClientFactory squareClientFactory,
        ILogger logger,
        ISettingService settingService,
        IWorkContext workContext,
        SquareAuthorizationHttpClient squareAuthorizationHttpClient)
    {
        _squareClientFactory = squareClientFactory;
        _logger = logger;
        _settingService = settingService;
        _workContext = workContext;
        _squareAuthorizationHttpClient = squareAuthorizationHttpClient;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Creates the Square Client
    /// </summary>
    /// <param name="storeId">Store identifier for which configuration should be loaded</param>
    /// <returns>The asynchronous task whose result contains the Square Client</returns>
    private Task<global::Square.SquareClient> CreateSquareClientAsync(int storeId)
    {
        return _squareClientFactory.CreateAsync(storeId);
    }

    private void ThrowErrorsIfExists(IReadOnlyList<Error> errors)
    {
        SquareResponse.ThrowIfErrors(errors);
    }

    private async Task<string> CatchExceptionAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        //log full error
        var errorMessage = exception.Message;
        await _logger.ErrorAsync($"Square payment error: {errorMessage}.", exception, await _workContext.GetCurrentCustomerAsync());

        //every operation reports errors as a raw body (Case B); read Square's error details from it
        if (exception is ApiException<RawError> apiException)
            errorMessage = ReadErrorDetails(apiException) ?? errorMessage;

        return errorMessage;
    }

    /// <summary>
    /// Logs a failed payment-workflow call, then tells apart the two ways it can fail. When Square answered and
    /// refused the request (a decline, an invalid value: a 4xx, or errors in the body), the error message is
    /// returned, as before. When the call itself failed (no connection, a timeout, a server error, a body that
    /// could not be read), a <see cref="SquareServiceException"/> carrying the original exception is thrown, so
    /// the caller can see the failure was Square's rather than the request's.
    /// </summary>
    /// <param name="exception">The exception the call threw</param>
    /// <returns>The asynchronous task whose result contains the error message of a refused request</returns>
    private async Task<string> RefusalOrThrowAsync(Exception exception)
    {
        var errorMessage = await CatchExceptionAsync(exception);

        var refusedBySquare = exception is NopException
            || exception is ApiException { StatusCode: >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError }
                and not ResponseDeserializationException;
        if (!refusedBySquare)
            throw new SquareServiceException(errorMessage, exception);

        return errorMessage;
    }

    /// <summary>
    /// Reads the "errors[].detail" of a Square error body; null when the body carries none
    /// </summary>
    private static string ReadErrorDetails(ApiException<RawError> exception)
    {
        try
        {
            var details = exception.Error.ReadAsJson<SquareErrorBody>()?.Errors?
                .Select(DescribeError)
                .Where(detail => !string.IsNullOrEmpty(detail))
                .ToList();

            return details?.Any() == true ? string.Join(";", details) : null;
        }
        catch (JsonException)
        {
            //not JSON (a gateway or proxy page): keep the exception message, which names the call and status
            return null;
        }
    }

    /// <summary>
    /// Describes a Square error for the customer: card declines get a plain message, anything else keeps Square's detail
    /// </summary>
    private static string DescribeError(Error error)
    {
        var code = error.Code;
        if (code == ErrorCode.CardDeclined || code == ErrorCode.GenericDecline)
            return "The card was declined. Please use a different card.";
        if (code == ErrorCode.InsufficientFunds)
            return "The card has insufficient funds.";
        if (code == ErrorCode.CvvFailure || code == ErrorCode.VerifyCvvFailure)
            return "The card security code (CVV) is incorrect.";
        if (code == ErrorCode.AddressVerificationFailure)
            return "The postal code does not match the card.";
        if (code == ErrorCode.InvalidExpiration || code == ErrorCode.CardExpired)
            return "The card has expired or its expiration date is invalid.";

        return error.Detail;
    }

    /// <summary>
    /// Square's error envelope: the "errors" member every response model declares
    /// </summary>
    private sealed record SquareErrorBody([property: JsonPropertyName("errors")] IReadOnlyList<Error> Errors);

    #endregion

    #region Methods

    #region Common

    /// <summary>
    /// Get selected active business locations
    /// </summary>
    /// <param name="storeId">Store identifier for which locations should be loaded</param>
    /// <returns>The asynchronous task whose result contains the Location; null when no location is selected,
    /// or the selected one is not active or cannot process cards</returns>
    /// <exception cref="SquareServiceException">Square could not be reached or did not answer usably</exception>
    public async Task<Location> GetSelectedActiveLocationAsync(int storeId)
    {
        var client = await CreateSquareClientAsync(storeId);
        var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeId);

        if (string.IsNullOrEmpty(settings.LocationId))
            return null;

        Location location;
        try
        {
            var locationResponse = await client.Locations.RetrieveLocation(new RetrieveLocationRequest { LocationId = settings.LocationId }) ?? throw new NopException("No service response");

            ThrowErrorsIfExists(locationResponse.Errors);

            location = locationResponse.Location;
        }
        catch (Exception exception)
        {
            //a failed call is not "no location": report it, so the caller can tell the two apart
            throw new SquareServiceException(await CatchExceptionAsync(exception), exception);
        }

        //an inactive location is a valid answer from Square, not a failure
        if (location == null
            || location.Status != LocationStatus.Active
            || (!location.Capabilities?.Contains(LocationCapability.CreditCardProcessing) ?? true))
        {
            await _logger.WarningAsync("Square payment warning: There are no selected active location for the account",
                customer: await _workContext.GetCurrentCustomerAsync());

            return null;
        }

        return location;
    }

    /// <summary>
    /// Gets active business locations
    /// </summary>
    /// <param name="storeId">Store identifier for which locations should be loaded</param>
    /// <returns>The asynchronous task whose result contains the List of location; empty when the account has no
    /// active location that can process cards</returns>
    /// <exception cref="SquareServiceException">Square could not be reached or did not answer usably</exception>
    public async Task<IList<Location>> GetActiveLocationsAsync(int storeId)
    {
        var client = await CreateSquareClientAsync(storeId);

        IEnumerable<Location> locations;
        try
        {
            var listLocationsResponse = await client.Locations.ListLocations();

            if (listLocationsResponse == null)
                throw new NopException("No service response");

            ThrowErrorsIfExists(listLocationsResponse.Errors);

            locations = listLocationsResponse.Locations;
        }
        catch (Exception exception)
        {
            //a failed call is not "no locations": report it, so the caller can tell the two apart
            throw new SquareServiceException(await CatchExceptionAsync(exception), exception);
        }

        //filter active locations and locations that can process credit cards
        var activeLocations = locations
            ?.Where(location => location?.Status == LocationStatus.Active &&
                (location.Capabilities?.Contains(LocationCapability.CreditCardProcessing) ?? false))
            .ToList() ?? new List<Location>();

        //an account with no usable location is a valid answer from Square, not a failure
        if (!activeLocations.Any())
            await _logger.WarningAsync("Square payment warning: There are no active locations for the account",
                customer: await _workContext.GetCurrentCustomerAsync());

        return activeLocations;
    }

    /// <summary>
    /// Get customer by identifier
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="storeId">Store identifier for which customer should be loaded</param>
    /// <returns>The asynchronous task whose result contains the Customer</returns>
    public async Task<Customer> GetCustomerAsync(string customerId, int storeId)
    {
        //whether passed customer identifier exists
        if (string.IsNullOrEmpty(customerId))
            return null;

        var client = await CreateSquareClientAsync(storeId);

        try
        {
            //get customer by identifier
            var retrieveCustomerResponse = await client.Customers.RetrieveCustomer(new RetrieveCustomerRequest { CustomerId = customerId });
            if (retrieveCustomerResponse == null)
                throw new NopException("No service response");

            ThrowErrorsIfExists(retrieveCustomerResponse.Errors);

            return retrieveCustomerResponse.Customer;
        }
        catch (Exception exception)
        {
            await CatchExceptionAsync(exception);

            return null;
        }
    }

    /// <summary>
    /// Create the new customer
    /// </summary>
    /// <param name="customerRequest">Request parameters to create customer</param>
    /// <param name="storeId">Store identifier for which customer should be created</param>
    /// <returns>The asynchronous task whose result contains the Customer</returns>
    public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest customerRequest, int storeId)
    {
        if (customerRequest == null)
            throw new ArgumentNullException(nameof(customerRequest));

        var client = await CreateSquareClientAsync(storeId);

        try
        {
            //create the new customer
            var createCustomerResponse = await client.Customers.CreateCustomer(new CreateCustomerOperationRequest { Body = customerRequest });
            if (createCustomerResponse == null)
                throw new NopException("No service response");

            ThrowErrorsIfExists(createCustomerResponse.Errors);

            return createCustomerResponse.Customer;
        }
        catch (Exception exception)
        {
            await CatchExceptionAsync(exception);

            return null;
        }
    }

    /// <summary>
    /// Create the new card of the customer
    /// </summary>
    /// <param name="cardRequest">Request parameters to create card of the customer</param>
    /// <param name="storeId">Store identifier for which customer card should be created</param>
    /// <returns>The asynchronous task whose result contains the Card</returns>
    public async Task<Card> CreateCustomerCardAsync(CreateCardRequest cardRequest, int storeId)
    {
        if (cardRequest == null)
            throw new ArgumentNullException(nameof(cardRequest));

        var client = await CreateSquareClientAsync(storeId);

        try
        {
            //create the new card of the customer
            var createCustomerCardResponse = await client.Cards.CreateCard(new CreateCardOperationRequest { Body = cardRequest });
            if (createCustomerCardResponse == null)
                throw new NopException("No service response");

            ThrowErrorsIfExists(createCustomerCardResponse.Errors);

            return createCustomerCardResponse.Card;
        }
        catch (Exception exception)
        {
            await CatchExceptionAsync(exception);

            return null;
        }
    }

    #endregion

    #region Payment workflow

    /// <summary>
    /// Creates a payment
    /// </summary>
    /// <param name="paymentRequest">Request parameters to create payment</param>
    /// <param name="storeId">Store identifier for which payment should be created</param>
    /// <returns>The asynchronous task whose result contains the Payment and/or errors if exist</returns>
    public async Task<(Payment, string)> CreatePaymentAsync(CreatePaymentRequest paymentRequest, int storeId)
    {
        if (paymentRequest == null)
            throw new ArgumentNullException(nameof(paymentRequest));

        var client = await CreateSquareClientAsync(storeId);

        try
        {
            var paymentResponse = await client.Payments.CreatePayment(new CreatePaymentOperationRequest { Body = paymentRequest });
            if (paymentResponse == null)
                throw new NopException("No service response");

            ThrowErrorsIfExists(paymentResponse.Errors);

            return (paymentResponse.Payment, null);
        }
        catch (Exception exception)
        {
            return (null, await RefusalOrThrowAsync(exception));
        }
    }

    /// <summary>
    /// Completes a payment
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="completePaymentRequest">Complete payment request</param>
    /// <param name="storeId">Store identifier for which payment should be completed</param>
    /// <returns>The asynchronous task whose result contains the True if the payment successfully completed; otherwise false. And/or errors if exist</returns>
    public async Task<(bool, string)> CompletePaymentAsync(string paymentId, CompletePaymentRequest completePaymentRequest, int storeId)
    {
        if (string.IsNullOrEmpty(paymentId))
            return (false, null);

        var client = await CreateSquareClientAsync(storeId);

        try
        {
            var paymentResponse = await client.Payments.CompletePayment(new CompletePaymentOperationRequest { PaymentId = paymentId, Body = completePaymentRequest }) ?? throw new NopException("No service response");

            ThrowErrorsIfExists(paymentResponse.Errors);

            //if there are no errors in the response, payment was successfully completed
            return (true, null);
        }
        catch (Exception exception)
        {
            return (false, await RefusalOrThrowAsync(exception));
        }
    }

    /// <summary>
    /// Cancels a payment
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="storeId">Store identifier for which payment should be canceled</param>
    /// <returns>The asynchronous task whose result contains the True if the payment successfully canceled; otherwise false. And/or errors if exist</returns>
    public async Task<(bool, string)> CancelPaymentAsync(string paymentId, int storeId)
    {
        if (string.IsNullOrEmpty(paymentId))
            return (false, null);

        var client = await CreateSquareClientAsync(storeId);

        try
        {
            var paymentResponse = await client.Payments.CancelPayment(new CancelPaymentRequest { PaymentId = paymentId });
            if (paymentResponse == null)
                throw new NopException("No service response");

            ThrowErrorsIfExists(paymentResponse.Errors);

            //if there are no errors in the response, payment was successfully canceled
            return (true, null);
        }
        catch (Exception exception)
        {
            return (false, await RefusalOrThrowAsync(exception));
        }
    }

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="refundPaymentRequest">Request parameters to create refund</param>
    /// <param name="storeId">Store identifier for which payment should be refunded</param>
    /// <returns>The asynchronous task whose result contains the Payment refund and/or errors if exist</returns>
    public async Task<(PaymentRefund, string)> RefundPaymentAsync(RefundPaymentRequest refundPaymentRequest, int storeId)
    {
        if (refundPaymentRequest == null)
            throw new ArgumentNullException(nameof(refundPaymentRequest));

        var client = await CreateSquareClientAsync(storeId);

        try
        {
            var refundPaymentResponse = await client.Refunds.RefundPayment(new RefundPaymentOperationRequest { Body = refundPaymentRequest });
            if (refundPaymentResponse == null)
                throw new NopException("No service response");

            ThrowErrorsIfExists(refundPaymentResponse.Errors);

            return (refundPaymentResponse.Refund, null);
        }
        catch (Exception exception)
        {
            return (null, await RefusalOrThrowAsync(exception));
        }
    }

    #endregion

    #region OAuth2 authorization

    /// <summary>
    /// Generate URL for the authorization permissions page
    /// </summary>
    /// <param name="storeId">Store identifier for which authorization url should be created</param>
    /// <returns>The asynchronous task whose result contains the URL</returns>
    public async Task<string> GenerateAuthorizeUrlAsync(int storeId)
    {
        var serviceUrl = "https://squareup.com/t/cmtp_performance/pr_cross_product/d_partnerships/p_nopcommerce/l_us/";

        //list of all available permission scopes
        var permissionScopes = new List<string>
        {
            //GET endpoints related to a merchant's business and location entities.
            "MERCHANT_PROFILE_READ",

            //GET endpoints related to transactions and refunds.
            "PAYMENTS_READ",

            //POST, PUT, and DELETE endpoints related to transactions and refunds
            "PAYMENTS_WRITE",

            //GET endpoints related to customer management.
            "CUSTOMERS_READ",

            //POST, PUT, and DELETE endpoints related to customer management.
            "CUSTOMERS_WRITE",

            //GET endpoints related to settlements (deposits).
            "SETTLEMENTS_READ",

            //GET endpoints related to a merchant's bank accounts.
            "BANK_ACCOUNTS_READ",

            //GET endpoints related to a merchant's item library.
            "ITEMS_READ",

            //POST, PUT, and DELETE endpoints related to a merchant's item library.
            "ITEMS_WRITE",

            //GET endpoints related to a merchant's orders.
            "ORDERS_READ",

            //POST, PUT, and DELETE endpoints related to a merchant's orders.
            "ORDERS_WRITE",

            //GET endpoints related to employee management.
            "EMPLOYEES_READ",

            //POST, PUT, and DELETE endpoints related to employee management.
            "EMPLOYEES_WRITE",

            //GET endpoints related to employee timecards.
            "TIMECARDS_READ",

            //POST, PUT, and DELETE endpoints related to employee timecards.
            "TIMECARDS_WRITE"
        };

        //request all of the permissions
        var requestingPermissions = string.Join(" ", permissionScopes);

        var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeId);

        //create query parameters for the request
        var queryParameters = new Dictionary<string, string>
        {
            //Route.
            ["route"] = "oauth2/authorize",

            //The application ID.
            ["client_id"] = settings.ApplicationId,

            //Indicates whether you want to receive an authorization code ("code") or an access token ("token").
            ["response_type"] = "code",

            //A space-separated list of the permissions your application is requesting. 
            ["scope"] = requestingPermissions,

            //The locale to present the Permission Request form in. Currently supported values are en-US, en-CA, es-US, fr-CA, and ja-JP.
            //["locale"] = string.Empty,

            //If "false", the Square merchant must log in to view the Permission Request form, even if they already have a valid user session.
            ["session"] = "false",

            //Include this parameter and verify its value to help protect against cross-site request forgery.
            ["state"] = settings.AccessTokenVerificationString,

            //The ID of the subscription plan to direct the merchant to sign up for, if any.
            //You can provide this parameter with no value to give a merchant the option to cancel an active subscription.
            //["plan_id"] = string.Empty,
        };

        //return generated URL
        return QueryHelpers.AddQueryString(serviceUrl, queryParameters);
    }

    /// <summary>
    /// Exchange the authorization code for an access token
    /// </summary>
    /// <param name="authorizationCode">Authorization code</param>
    /// <param name="storeId">Store identifier for which access token should be obtained</param>
    /// <returns>The asynchronous task whose result contains the Access and refresh tokens</returns>
    public async Task<(string AccessToken, string RefreshToken)> ObtainAccessTokenAsync(string authorizationCode, int storeId)
    {
        return await _squareAuthorizationHttpClient.ObtainAccessTokenAsync(authorizationCode, storeId);
    }

    /// <summary>
    /// Renew the expired access token
    /// </summary>
    /// <param name="storeId">Store identifier for which access token should be updated</param>
    /// <returns>The asynchronous task whose result contains the Access and refresh tokens</returns>
    public async Task<(string AccessToken, string RefreshToken)> RenewAccessTokenAsync(int storeId)
    {
        return await _squareAuthorizationHttpClient.RenewAccessTokenAsync(storeId);
    }

    /// <summary>
    /// Revoke all access tokens
    /// </summary>
    /// <param name="storeId">Store identifier for which access token should be revoked</param>
    /// <returns>The asynchronous task whose result contains the True if tokens were successfully revoked; otherwise false</returns>
    public async Task<bool> RevokeAccessTokensAsync(int storeId)
    {
        return await _squareAuthorizationHttpClient.RevokeAccessTokensAsync(storeId);
    }

    #endregion

    #endregion
}