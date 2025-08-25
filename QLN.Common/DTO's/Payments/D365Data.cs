using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class D365Data
    {
        [JsonPropertyName("paymentInfo")]
        public PaymentEntity PaymentInfo { get; set; }

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = D365PaymentOperations.CHECKOUT;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("item")]
        public ItemData? Item { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("user")]
        public UserData? User { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("productDuration")]
        public ProductDurationData? ProductDuration { get; set; }

    }

    public class ItemData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class UserData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }
    }

    public class ProductDurationData
    {
        [JsonPropertyName("startDate_dd_mm_yyyy")]
        public DateTime StartDate { get; set; }
        [JsonPropertyName("endDate_dd_mm_yyyy")]
        public DateTime EndDate { get; set; }
    }

}
