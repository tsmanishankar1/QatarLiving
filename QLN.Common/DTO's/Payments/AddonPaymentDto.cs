using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s.Payments
{

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

        public int? PromoteBudget { get; set; }

        /// <summary>
        /// The duration in days
        /// </summary>
        public int? Duration { get; set; }
    }
}