using Square.Core.Models;
using Square.Models;

namespace Nop.Plugin.Payments.Square.Extensions;

public static class CreatePaymentRequestExtensions
{
    #region Methods

    /// <summary>
    /// Returns a copy of the request that also sends Square's "integration_id" field
    /// </summary>
    /// <remarks>
    /// CreatePaymentRequest does not declare "integration_id", so it travels in the model's extension-data bag,
    /// whose entries are written into the request's JSON object under their wire names. The copy gets its own bag,
    /// so the original request is left unchanged.
    /// </remarks>
    /// <param name="paymentRequest">Payment request</param>
    /// <param name="integrationId">Integration ID; nothing is added when it is empty</param>
    /// <returns>The payment request to send</returns>
    public static CreatePaymentRequest WithIntegrationId(this CreatePaymentRequest paymentRequest, string integrationId)
    {
        ArgumentNullException.ThrowIfNull(paymentRequest);

        if (string.IsNullOrEmpty(integrationId))
            return paymentRequest;

        var additionalProperties = new AdditionalProperties();
        foreach (var (key, value) in paymentRequest.AdditionalProperties)
            additionalProperties[key] = value;
        additionalProperties.Set("integration_id", integrationId);

        return paymentRequest with { AdditionalProperties = additionalProperties };
    }

    #endregion
}
