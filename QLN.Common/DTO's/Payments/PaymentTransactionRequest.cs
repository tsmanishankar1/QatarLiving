using QLN.Common.Infrastructure.Subscriptions;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class PaymentTransactionRequest
    {
        [JsonPropertyName("transaction_id")]
        public required string TransactionId { get; set; }

        [JsonPropertyName("order_id")]
        public required string OrderId { get; set; }

        [JsonPropertyName("card_token")]
        public string? CardToken { get; set; }

        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("response_Code")]
        public string? ResponseCode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        [JsonPropertyName("vertical")]
        public Vertical Vertical { get; set; }

        [JsonPropertyName("subvertical")]
        public SubVertical? SubVertical { get; set; }
    }
}
