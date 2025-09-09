using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Payments
{
    public class PaymentVerificationFailureResponse
    {
        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; } = "Resourse not found";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "Specified payment does not exists";
    }
}