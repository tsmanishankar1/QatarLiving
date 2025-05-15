namespace QLN.Web.Shared.Components.Classifieds.DealsCard
{
    public partial class DealsCard
    {
        public class DealModel
        {
            public string ImageUrl { get; set; } = string.Empty;
            public string StoreLogoUrl { get; set; } = string.Empty;
            public string StoreName { get; set; } = string.Empty;
            public string StoreDescription { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string ExpiryText { get; set; } = string.Empty;
        }
    }
}
