using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    using System.Text.Json.Serialization;

    public class FatoraPaymentRequest
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("order_id")]
        public int? OrderId { get; set; }

        [JsonPropertyName("client")]
        public FaturaClientInfo Client { get; set; } = new();

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("success_url")]
        public string? SuccessUrl { get; set; }

        [JsonPropertyName("failure_url")]
        public string? FailureUrl { get; set; }
    }

    public class FaturaClientInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
    }
}
