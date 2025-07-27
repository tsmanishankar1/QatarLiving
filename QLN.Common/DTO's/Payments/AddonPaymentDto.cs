using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s.Payments
{
    public enum ProductType
    {
        PUBLISH = 100,
        FEATURE_1W = 101,
        FEATURE_2W = 102,
        FEATURE_1M = 103,
        PUBLISH_FEATURE_1W = 104,
        PUBLISH_FEATURE_2W = 105,
        PUBLISH_FEATURE_1M = 106,
        SUBSCRIPTION = 107,
        ADDON_COMBO = 108,
        ADDON_FEATURE = 109,
        ADDON_REFRESH = 110
    }

    public class AddonPaymentDto
    {
        /// <summary>
        /// D365 Lookup id
        /// </summary>
        [Required]
        public int D365Id { get; set; }

        /// <summary>
        /// Feature Budget for the addon
        /// </summary>
        public double? FeatureBudget { get; set; }

        /// <summary>
        /// Type of the product to purchase
        /// </summary>
        [Required]
        public ProductType ProductType { get; set; }

        /// <summary>
        /// Refresh Budget for the addon
        /// </summary>
        public double? RefreshBudget { get; set; }

        /// <summary>
        /// The duration in days
        /// </summary>
        public int? Duration { get; set; }
    }
}