using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class OrderItem
    {
        [JsonPropertyName("QLUserId")]
        public string QLUserId { get; set; }

        [JsonPropertyName("QLUserName")]
        public string QLUserName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        [JsonPropertyName("classification")]
        public string Classification { get; set; }

        [JsonPropertyName("subClassification")]
        public string SubClassification { get; set; }

        [JsonPropertyName("QLOrderId")]
        public string QLOrderId { get; set; }

        [JsonPropertyName("orderType")]
        public string OrderType { get; set; }

        [JsonPropertyName("itemId")]
        public string ItemId { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("companyId")]
        public string CompanyId { get; set; }

        [JsonPropertyName("addId")]
        public long? AddId { get; set; }
    }
}
