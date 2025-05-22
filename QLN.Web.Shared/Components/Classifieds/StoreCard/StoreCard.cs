namespace QLN.Web.Shared.Components.Classifieds.StoreCard
{
    public partial class StoreCard
    {
        public class StoreItem
        {
            public string StoreName { get; set; } = string.Empty;
            public string LogoUrl { get; set; } = string.Empty;
            public int ItemCount { get; set; }
        }
    }
}
