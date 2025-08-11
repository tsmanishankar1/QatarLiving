using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{

    public class LegacySubscriptionDrupal
    {
        [JsonPropertyName("item")]
        public SubscriptionItem Item { get; set; }
    }

    public class SubscriptionItem
    {
        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("subscription_id")]
        public int SubscriptionId { get; set; }

        [JsonPropertyName("reference_id")]
        public string ReferenceId { get; set; }

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string EndDate { get; set; }

        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("product")]
        public string Product { get; set; }

        [JsonPropertyName("package_id")]
        public int PackageId { get; set; }

        [JsonPropertyName("package")]
        public string Package { get; set; }

        [JsonPropertyName("ads_limit")]
        public string AdsLimit { get; set; }

        [JsonPropertyName("ads_limit_daily")]
        public int AdsLimitDaily { get; set; }

        [JsonPropertyName("refresh_limit")]
        public string RefreshLimit { get; set; }

        [JsonPropertyName("refresh_limit_daily")]
        public string RefreshLimitDaily { get; set; }

        [JsonPropertyName("sticky_limit")]
        public string StickyLimit { get; set; }

        [JsonPropertyName("feature_limit")]
        public string FeatureLimit { get; set; }

        [JsonPropertyName("product_class")]
        public string ProductClass { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class LegacySubscriptionDto
    {
        [JsonPropertyName("drupal")]
        public LegacySubscriptionDrupal Drupal { get; set; }
    }
}
