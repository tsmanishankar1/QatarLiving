using System.Text.Json.Serialization;

namespace QLN.Web.Shared.Models
{
    public class ClassifiedsDashboardModel
    {
        public class StatItem
        {
            public string Title { get; set; }
            public string Value { get; set; }
            public string Icon { get; set; }
        }
        public class AdListResponse
        {
            public int Total { get; set; }
            public List<AdModal> Items { get; set; } = new();
        }

        public class ItemDashboardResponse
        {
            [JsonPropertyName("itemsDashboard")]
            public ItemsDashboard ItemsDashboard { get; set; }

        
        }
        public class PreLovedDashboardResponse
        {
            [JsonPropertyName("prelovedDashboard")]
            public PreLovedDashboard preLovedDashboard { get; set; }

            
        }

        public class PreLovedDashboard
        {
            [JsonPropertyName("publishedAds")]
            public int PublishedAds { get; set; }

            [JsonPropertyName("promotedAds")]
            public int PromotedAds { get; set; }

            [JsonPropertyName("featuredAds")]
            public int FeaturedAds { get; set; }

            [JsonPropertyName("refreshes")]
            public int Refreshes { get; set; }

            [JsonPropertyName("remainingRefreshes")]
            public int RemainingRefreshes { get; set; }

            [JsonPropertyName("totalAllowedRefreshes")]
            public int TotalAllowedRefreshes { get; set; }


            [JsonPropertyName("impressions")]
            public int Impressions { get; set; }

            [JsonPropertyName("views")]
            public int Views { get; set; }

            [JsonPropertyName("whatsAppClicks")]
            public int WhatsAppClicks { get; set; }

            [JsonPropertyName("calls")]
            public int Calls { get; set; }
        }

        public class PreLovedDashboardAds
        {
            [JsonPropertyName("publishedAds")]
            public List<AdModal> PublishedAds { get; set; }

            [JsonPropertyName("unpublishedAds")]
            public List<AdModal> UnpublishedAds { get; set; }
        }

        public class ItemsDashboard
        {
            [JsonPropertyName("publishedAds")]
            public int PublishedAds { get; set; }

            [JsonPropertyName("promotedAds")]
            public int PromotedAds { get; set; }

            [JsonPropertyName("featuredAds")]
            public int FeaturedAds { get; set; }

            [JsonPropertyName("refreshes")]
            public int Refreshes { get; set; }

            [JsonPropertyName("remainingRefreshes")]
            public int RemainingRefreshes { get; set; }

            [JsonPropertyName("totalAllowedRefreshes")]
            public int TotalAllowedRefreshes { get; set; }

            [JsonPropertyName("refreshExpiry")]
            public DateTime RefreshExpiry { get; set; }

            [JsonPropertyName("impressions")]
            public int Impressions { get; set; }

            [JsonPropertyName("views")]
            public int Views { get; set; }

            [JsonPropertyName("whatsAppClicks")]
            public int WhatsAppClicks { get; set; }

            [JsonPropertyName("calls")]
            public int Calls { get; set; }
        }

        public class ItemsAds
        {
            [JsonPropertyName("publishedAds")]
            public List<AdModal> PublishedAds { get; set; }

            [JsonPropertyName("unpublishedAds")]
            public List<AdModal> UnpublishedAds { get; set; }
        }

        public class AdModal
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("category")]
            public string Category { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("zone")]
            public string Location { get; set; }

            [JsonPropertyName("subVertical")]
            public string SubVertical { get; set; }

            [JsonPropertyName("price")]
            public decimal Price { get; set; }

            [JsonPropertyName("priceType")]
            public string PriceType { get; set; }

            [JsonPropertyName("phoneNumber")]
            public string PhoneNumber { get; set; }

            [JsonPropertyName("whatsappNumber")]
            public string WhatsappNumber { get; set; }

            [JsonPropertyName("createdAt")]
            public DateTime CreatedDate { get; set; }

            [JsonPropertyName("expiryDate")]
            public DateTime ExpiryDate { get; set; }

            [JsonPropertyName("userId")]
            public string UserId { get; set; }

            [JsonPropertyName("isFeatured")]
            public bool IsFeatured { get; set; }

            [JsonPropertyName("isPromoted")]
            public bool IsPromoted { get; set; }

            [JsonPropertyName("isRefreshed")]
            public bool IsRefreshed { get; set; }

            [JsonPropertyName("refreshExpiry")]
            public DateTime? RefreshExpiry { get; set; }

            [JsonPropertyName("remainingRefreshes")]
            public int RemainingRefreshes { get; set; }

            [JsonPropertyName("totalAllowedRefreshes")]
            public int TotalAllowedRefreshes { get; set; }

            [JsonPropertyName("impressions")]
            public int Impressions { get; set; }

            [JsonPropertyName("views")]
            public int Views { get; set; }

            [JsonPropertyName("calls")]
            public int Calls { get; set; }

            [JsonPropertyName("whatsAppClicks")]
            public int WhatsAppClicks { get; set; }

            [JsonPropertyName("shares")]
            public int Shares { get; set; }

            [JsonPropertyName("saves")]
            public int Saves { get; set; }

            [JsonPropertyName("status")]
            public int Status { get; set; }

            [JsonPropertyName("imageUrls")]
            public List<ImageUrlItem> ImageUrls { get; set; }
            public bool IsSelected { get; set; }
        }
        public class ImageUrlItem
        {
            [JsonPropertyName("adImageFileNames")]
            public string AdImageFileNames { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("order")]
            public int Order { get; set; }
        }

    }
}
