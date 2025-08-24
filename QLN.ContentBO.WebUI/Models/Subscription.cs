namespace QLN.ContentBO.WebUI.Models
{
    public class Subscription
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ProductType { get; set; }
        public int Vertical { get; set; }
        public int SubVertical { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public SubscriptionConstraint Constraints { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SubscriptionConstraint
    {
        public decimal? AdsBudget { get; set; }
        public decimal? FeaturedBudget { get; set; }
        public decimal? PromotedBudget { get; set; }
        public decimal? RefreshBudgetPerDay { get; set; }
        public decimal? RefreshBudgetPerAd { get; set; }
        public string Duration { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public bool IsAddOn { get; set; }
        public bool PayToPublish { get; set; }
        public bool PayToPromote { get; set; }
        public bool PayToFeature { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
