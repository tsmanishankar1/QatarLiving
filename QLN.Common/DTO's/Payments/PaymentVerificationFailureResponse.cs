namespace QLN.Common.DTO_s.Payments
{
    public class PaymentVerificationFailureResponse
    {
        public string ErrorCode { get; set; } = "RESOURCE_NOT_FOUND";
        public string Description { get; set; } = "Specified payment does not exist.";
    }
}