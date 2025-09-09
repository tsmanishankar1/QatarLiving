using QLN.Common.Infrastructure.Subscriptions;

namespace QLN.Common.DTO_s.Payments
{
    /// <summary>
    /// New multi-product payment request (V2)
    /// </summary>
    public class ExternalPaymentRequest
    {
        /// <summary> Ad Id against which the payment has to be done (e.g., Pay-To-Publish or Addon linking) </summary>
        public long? AdId { get; set; }

        /// <summary> Existing subscription to target (for add-ons), optional </summary>
        public Guid? SubscriptionId { get; set; }

        /// <summary> Which site section this payment is for </summary>
        public Vertical Vertical { get; set; }

        public SubVertical? SubVertical { get; set; }

        /// <summary> Auth/user context </summary>
        public UserReqDto? User { get; set; }

        /// <summary>
        /// Products being purchased in this order (subscription, publish, add-ons, etc.).
        /// Amount will be computed from these unless explicitly provided.
        /// </summary>
        public List<PaymentProductDto> Products { get; set; } = new();

        /// <summary>
        /// Optional override. If null, server will compute from Products (sum of UnitPrice * Quantity).
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary> Filled server-side after persisting the Payment row </summary>
        public int? OrderId { get; set; }
    }

    /// <summary>
    /// One product line in the payment
    /// </summary>
    public class PaymentProductDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public ProductType ProductType { get; set; }

        /// <summary>
        /// Unit price (optional). If omitted, server will look up from Products table by ProductCode.
        /// </summary>
        public decimal? UnitPrice { get; set; }

    }
    public class ProductProcessingResult
    {
        public Guid? SubscriptionId { get; set; }
        public Guid? AddonId { get; set; }
    }

    public class SubscriptionValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static SubscriptionValidationResult Success() => new() { IsValid = true };

        public static SubscriptionValidationResult Failure(string errorMessage) => new()
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}
