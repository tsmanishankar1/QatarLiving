using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class PaymentResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("result")]
        public FaturaPaymentResult? Result { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("error")]
        public FaturaPaymentError? Error { get; set; }
    }

    public class FaturaPaymentResult
    {
        [JsonPropertyName("checkout_url")]
        public string? CheckOutUrl { get; set; }
    }

    public class FaturaPaymentError
    {
        [JsonPropertyName("error_code")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
