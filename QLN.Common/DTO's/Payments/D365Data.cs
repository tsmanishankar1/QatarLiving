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
        public PaymentEntity PaymentInfo { get; set; }
        public string Operation { get; set; } = D365PaymentOperations.CHECKOUT;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ItemData? Item { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UserData? User { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ProductDurationData? ProductDuration { get; set; }

    }

    public class ItemData
    {
        public string Id { get; set; }
        public int Quantity { get; set; }
    }

    public class UserData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
    }

    public class ProductDurationData
    {
        public DateTime? StartDate_dd_mm_yyyy { get; set; }
        public DateTime? EndDate_dd_mm_yyyy { get; set; }
    }

    public static class D365PaymentOperations
    {
        public const string CHECKOUT = "CHECKOUT";
        public const string SUCCESS = "SUCCESS";
        public const string FAILURE = "FAILURE";
    }

}
