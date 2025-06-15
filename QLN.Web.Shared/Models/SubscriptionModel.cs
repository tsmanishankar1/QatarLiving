using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Models
{

    public class SubscriptionModel
    {
        [Required(ErrorMessage = "Subscription name is required")]
        public string SubscriptionName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Currency is required")]
        public string Currency { get; set; } = "QAR";

        [Required(ErrorMessage = "Duration is required")]
        public string Duration { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vertical type is required")]
        public string VerticalType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sub category is required")]
        public string SubCategory { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<SubscriptionPlan> Subscriptions { get; set; } = new();
    }

}


