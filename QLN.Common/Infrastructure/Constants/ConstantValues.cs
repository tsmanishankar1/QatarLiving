
namespace QLN.Common.Infrastructure.Constants
{
    public class ConstantValues
    {
        public const string QLNProvider = "QLN";

        public const string RefreshToken = "refresh_token";
        public const string RefreshTokenExpiry = "refresh_token_expiry";
        public const string Email = "email";
        public const string Phone = "phone";
        public const string ByPassEmail = "testuser@qatarliving.com";
        public const string ByPassMobile = "0000000000";
        public const string ByPass2FA = "000000";
        //Company Constants
        public const string CompanyStoreName = "companystatestore";
        public const string CompanyIndexKey = "company-index";
        public const string CompanyServiceAppId = "qln-company-ms";
        // Classifieds Constants
        public const string ClassifiedsVertical = "classifieds";
        public const string DocTypeStore = "Store";
        public const string DocTypeCategory = "Category";
        public const string DocTypeAd = "Ad";
        public const string DocTypeBanner = "Banner";
        //Index constants
        public const string LandingBackOffice = "landingbackoffice";
        public const string Analytics = "analytics";
        public const string PubSubName = "pubsub";
        public static class EntityTypes
        {
            public const string HeroBanner = "HeroBanner";
            public const string TakeOverBanner = "TakeOverBanner";
            public const string FeaturedItems = "FeaturedItems";
            public const string FeaturedServices = "FeaturedServices";
            public const string FeaturedCategory = "FeaturedCategory";
            public const string FeaturedStores = "FeaturedStores";
            public const string Category = "Category";
            public const string SeasonalPick = "SeasonalPick";
            public const string SocialMediaLink = "SocialMediaLink";
            public const string SocialPostSection = "SocialPostSection";
            public const string SocialMediaVideos = "SocialMediaVideos";
            public const string FaqItem = "FaqItem";
            public const string ReadyToGrow = "ReadyToGrow";
            public const string PopularSearch = "PopularSearch";

        }
        public static class EntityRoutes
        {
            public const string HeroBanner = "hero-banner";
            public const string TakeOverBanner = "take-over-banner";
            public const string FeaturedItems = "featured-items";
            public const string FeaturedServices = "featured-services";
            public const string FeaturedCategory = "featured-category";
            public const string FeaturedStores = "featured-stores";
            public const string Category = "category";
            public const string SeasonalPick = "seasonal-pick";
            public const string SocialMediaLink = "social-media-link";
            public const string SocialPostSection = "social-post-section";
            public const string SocialMediaVideos = "social-media-videos";
            public const string FaqItem = "faq-item";
            public const string ReadyToGrow = "ready-to-grow";
            public const string PopularSearch = "popular-search";
        }
        public static class ServiceAppIds
        {
            public const string ClassifiedServiceApp = "qln-classified-ms";
            public const string SearchServiceApp = "qln-search-ms";
        }
        public static class StateStoreNames
        {
            public const string LandingBackOfficeStore = "landingbackofficestore";
            public const string LandingBackOfficeKey = "landing-backoffice-keys";
            public const string UnifiedStore = "adstore";
            public const string UnifiedIndexKey = "ad-index";
            public const string ItemsIndexKey = "items-ad-index";
            public const string PrelovedIndexKey = "preloved-index";
            public const string CollectiblesIndexKey = "collectibles-index";
            public const string DealsIndexKey = "deals-index";
            public const string ItemsCategoryIndexKey = "items-category-index";
            public const string PrelovedCategoryIndexKey = "preloved-category-index";
            public const string CollectiblesCategoryIndexKey = "collectibles-category-index";
            public const string DealsCategoryIndexKey = "deals-category-index";
        }
        public static class PubSubTopics
        {
            public const string IndexUpdates = "index-updates";
        }
        public static class Verticals
        {
            public const string Classifieds = "classifieds";
            public const string Services = "Services";
        }
        public static class V2ContentEvents
        {
            public const string ContentStoreName = "v2eventstatestore";
            public const string EventIndexKey = "event-index";
            // renaming this to content as all content will go to this MS - it may have multiple state stores for data though, so leaving in place the event statestore
            public const string ContentServiceAppId = "qln-content-ms"; 
        }
        public static class V2ContentNews
        {
            public const string v2contentnews = "contentstatestore";
            public const string v2contentServiceAppId = "qln-content-ms";
        }
    }
}
