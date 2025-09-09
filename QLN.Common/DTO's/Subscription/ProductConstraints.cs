using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Subscription
{
    public class ProductConstraints
    {
        public int? AdsBudget { get; set; } // Total number of ads allowed
        public int? FeaturedBudget { get; set; } // Featured ads count
        public int? PromotedBudget { get; set; } // Promoted ads count
        public int? RefreshBudgetPerDay { get; set; } // Refreshes per day (if applicable)
        public int? RefreshBudgetPerAd { get; set; } // Refresh budget per ad (optional)
        public TimeSpan? Duration { get; set; } // "1 Month", "6 Months", "12 Months", etc.
        public string? Scope { get; set; } // e.g., "All", "Per L2-category", or specific category
        public bool? IsAddOn { get; set; } // True if it's an addon
        public bool? PayToPublish { get; set; } // Used for pay2publish
        public bool? PayToPromote { get; set; }
        public bool? PayToFeature { get; set; }
        public string? Remarks { get; set; }
        public List<CategoryQuota> CategoryQuotas { get; set; } = new List<CategoryQuota>();
    }
    public class CategoryQuota
    {
        public string Category { get; set; } = string.Empty; // e.g., "Electronics"
        public string? L1Category { get; set; } // e.g., "Home appliances"
        public string? L2Category { get; set; } // e.g., "ACs"
        public int AdsBudget { get; set; } // Ads allowed in this category/subcategory
        public int FeaturedBudget { get; set; } = 0; // Featured ads in this category
        public int PromotedBudget { get; set; } = 0; // Promoted ads in this category
        public int RefreshBudget { get; set; } = 0; // Refresh budget for this category
    }

}
