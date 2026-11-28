using System.Diagnostics.CodeAnalysis;
using Nop.Core;
using Square.Models;
using Square.Models.Enums;
using Square.Requests.Payments;
using Square.Requests.Refunds;
using Square.Requests.Transactions;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Reference to a payment taken with Square's legacy Transactions API (Charge endpoint).
/// Orders paid before the plugin moved to the Payments API store it as "legacy:{locationId}:{transactionId}"
/// in their authorization/capture transaction identifier.
/// </summary>
/// <param name="LocationId">Square location the transaction belongs to</param>
/// <param name="TransactionId">Square transaction identifier</param>
public record LegacyTransactionReference(string LocationId, string TransactionId)
{
    /// <summary>
    /// Prefix of a stored legacy transaction reference
    /// </summary>
    public const string Prefix = "legacy:";

    /// <summary>
    /// Parses a stored transaction identifier
    /// </summary>
    /// <param name="value">Stored transaction identifier</param>
    /// <param name="reference">The legacy reference, when the identifier is one</param>
    /// <returns>True when the identifier is a legacy transaction reference</returns>
    public static bool TryParse(string value, [NotNullWhen(true)] out LegacyTransactionReference reference)
    {
        reference = null;
        if (string.IsNullOrEmpty(value) || !value.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        var parts = value[Prefix.Length..].Split(':');
        if (parts.Length != 2 || parts.Any(string.IsNullOrEmpty))
            return false;

        reference = new LegacyTransactionReference(parts[0], parts[1]);
        return true;
    }

    public override string ToString() => $"{Prefix}{LocationId}:{TransactionId}";
}

/// <summary>
/// What is left to refund on one tender of a legacy transaction
/// </summary>
/// <param name="TenderId">Tender identifier</param>
/// <param name="Currency">Currency of the tender</param>
/// <param name="Paid">Amount paid with the tender, in the smallest currency unit</param>
/// <param name="Refundable">Amount that can still be refunded, in the smallest currency unit</param>
public record LegacyTenderBalance(string TenderId, string Currency, long Paid, long Refundable);

/// <summary>
/// Captures, voids and refunds orders paid with Square's legacy Transactions API
/// </summary>
public class LegacyTransactionService
{
    #region Fields

    private readonly ISquareClientFactory _squareClientFactory;

    #endregion

    #region Ctor

    public LegacyTransactionService(ISquareClientFactory squareClientFactory)
    {
        _squareClientFactory = squareClientFactory;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Whether a refund counts against the tender (pending or approved; rejected and failed refunds returned nothing)
    /// </summary>
    private static bool IsActiveRefund(Refund refund)
    {
        return refund.Status == RefundStatus.Pending || refund.Status == RefundStatus.Approved;
    }

    /// <summary>
    /// Retrieves a legacy transaction
    /// </summary>
    private static async Task<Transaction> RetrieveTransactionAsync(global::Square.SquareClient client, LegacyTransactionReference reference)
    {
        var response = await client.Transactions.RetrieveTransaction(new RetrieveTransactionRequest
        {
            LocationId = reference.LocationId,
            TransactionId = reference.TransactionId
        }) ?? throw new NopException("No service response");

        SquareResponse.ThrowIfErrors(response.Errors);

        return response.Transaction ?? throw new NopException($"Transaction {reference.TransactionId} was not found");
    }

    /// <summary>
    /// Gets the identifiers of the tenders of a legacy transaction
    /// </summary>
    private static async Task<IList<string>> GetTenderIdsAsync(global::Square.SquareClient client, LegacyTransactionReference reference)
    {
        var transaction = await RetrieveTransactionAsync(client, reference);
        var tenderIds = (transaction.Tenders ?? [])
            .Select(tender => tender.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        return tenderIds.Any() ? tenderIds : throw new NopException($"Transaction {reference.TransactionId} has no tenders");
    }

    #endregion

    #region Methods

    /// <summary>
    /// Captures a legacy transaction created with delayed capture
    /// </summary>
    /// <remarks>
    /// Square no longer offers capturing a transaction, so each of its tenders is completed as the payment it is
    /// in the Payments API (a legacy tender's identifier is the identifier of its payment).
    /// </remarks>
    /// <param name="reference">Legacy transaction reference</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task CaptureAsync(LegacyTransactionReference reference, int storeId)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var client = await _squareClientFactory.CreateAsync(storeId);
        foreach (var tenderId in await GetTenderIdsAsync(client, reference))
        {
            var response = await client.Payments.CompletePayment(new CompletePaymentOperationRequest
            {
                PaymentId = tenderId,
                Body = new CompletePaymentRequest()
            }) ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);
        }
    }

    /// <summary>
    /// Voids a legacy transaction created with delayed capture
    /// </summary>
    /// <remarks>
    /// Square no longer offers voiding a transaction, so each of its tenders is canceled as the payment it is
    /// in the Payments API (a legacy tender's identifier is the identifier of its payment).
    /// </remarks>
    /// <param name="reference">Legacy transaction reference</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task VoidAsync(LegacyTransactionReference reference, int storeId)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var client = await _squareClientFactory.CreateAsync(storeId);
        foreach (var tenderId in await GetTenderIdsAsync(client, reference))
        {
            var response = await client.Payments.CancelPayment(new CancelPaymentRequest { PaymentId = tenderId })
                ?? throw new NopException("No service response");

            SquareResponse.ThrowIfErrors(response.Errors);
        }
    }

    /// <summary>
    /// Gets what can still be refunded on each tender of a legacy transaction
    /// </summary>
    /// <param name="reference">Legacy transaction reference</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>The asynchronous task whose result contains the balance of each tender</returns>
    public async Task<IList<LegacyTenderBalance>> GetRefundableBalancesAsync(LegacyTransactionReference reference, int storeId)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var client = await _squareClientFactory.CreateAsync(storeId);
        var transaction = await RetrieveTransactionAsync(client, reference);
        var activeRefunds = (transaction.Refunds ?? []).Where(IsActiveRefund).ToList();

        //a refund may not name its tender; it was still taken from one of the tenders, so it is held against
        //each of them: what is reported refundable on a tender can then always be refunded
        var unattributed = activeRefunds
            .Where(refund => string.IsNullOrEmpty(refund.TenderId))
            .Sum(refund => refund.AmountMoney.Amount ?? 0);

        return (transaction.Tenders ?? [])
            .Select(tender =>
            {
                var paid = tender.AmountMoney?.Amount ?? 0;
                var refunded = activeRefunds
                    .Where(refund => !string.IsNullOrEmpty(refund.TenderId) && refund.TenderId == tender.Id)
                    .Sum(refund => refund.AmountMoney.Amount ?? 0);

                return new LegacyTenderBalance(tender.Id, tender.AmountMoney?.Currency?.Value, paid, Math.Max(0, paid - refunded - unattributed));
            })
            .ToList();
    }

    /// <summary>
    /// Refunds part or all of a legacy transaction
    /// </summary>
    /// <param name="reference">Legacy transaction reference</param>
    /// <param name="amount">Amount to refund, in the smallest currency unit</param>
    /// <param name="currency">Currency code</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>The asynchronous task whose result contains the refund status reported by Square</returns>
    public async Task<string> RefundAsync(LegacyTransactionReference reference, long amount, string currency, int storeId)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "The refund amount must be positive");

        //never refund more than is left on the tender
        var balances = await GetRefundableBalancesAsync(reference, storeId);
        var tender = balances.FirstOrDefault(balance => balance.Refundable >= amount)
            ?? throw new NopException($"The refund exceeds the refundable amount of transaction {reference.TransactionId} ({balances.Sum(balance => balance.Refundable)})");

        //a legacy tender's identifier is the identifier of its payment in the Payments API
        var client = await _squareClientFactory.CreateAsync(storeId);
        var response = await client.Refunds.RefundPayment(new RefundPaymentOperationRequest
        {
            Body = new RefundPaymentRequest
            {
                IdempotencyKey = Guid.NewGuid().ToString(),
                PaymentId = tender.TenderId,
                AmountMoney = new Money { Amount = amount, Currency = SquareLookups.GetCurrency(currency ?? tender.Currency) },
                Reason = $"nopCommerce refund of legacy transaction {reference.TransactionId}"
            }
        }) ?? throw new NopException("No service response");

        SquareResponse.ThrowIfErrors(response.Errors);

        return response.Refund?.Status;
    }

    #endregion
}
