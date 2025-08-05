using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using QLN.Common.Infrastructure.Subscriptions;

namespace QLN.Common.DTO_s.Payments
{

    namespace QLN.Common.DTO_s.Payments
    {
        public class PaymentRedirect
        {
            [JsonPropertyName("vertical")]
            public Vertical Vertical { get; set; }

            [JsonPropertyName("subscriptionTypeId")]
            public SubscriptionType SubscriptionTypeId { get; set; }

            [JsonPropertyName("productType")]
            public ProductType? ProductType { get; set; }

            [JsonPropertyName("userId")]
            public int? UserId { get; set; }

            [JsonPropertyName("adId")]
            public int? AdId { get; set; }
        }


    }
}
