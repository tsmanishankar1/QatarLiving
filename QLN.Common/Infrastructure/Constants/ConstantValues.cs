
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
        // Classifieds Constants
        public const string ClassifiedsVertical = "classifieds";
        public const string DocTypeStore = "Store";
        public const string DocTypeCategory = "Category";
        public const string DocTypeAd = "Ad";
        public const string DocTypeBanner = "Banner";
        public const string SubscriptionPrefix = "qln-subscription-actor";

        //Drupal user AutoComplete
        public const string AutocompleteUserPath = "/qlnapi/user/autocomplete";

        //Index constants
        public static class IndexNames
        {
            public const string ClassifiedsItemsIndex = "classifiedsitems";
            public const string ClassifiedsPrelovedIndex = "classifiedspreloved";
            public const string ClassifiedsCollectiblesIndex = "classifiedscollectibles";
            public const string ClassifiedsDealsIndex = "classifiedsdeals";
            public const string ServicesIndex = "services";
            public const string ContentNewsIndex = "contentnews";
            public const string ContentEventsIndex = "contentevents";
            public const string ContentCommunityIndex = "contentcommunity";
            public const string LandingBackOfficeIndex = "landingbackoffice";
            public const string AnalyticsIndex = "analytics";
        }

        public const string PubSubName = "pubsub";

        public const int DefaultPageSize = 50;
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
            public const string SubscriptionApp = "qln-subscription-actor";
        }
        public static class StateStoreNames
        {
            public const string LandingBackOfficeStore = "landingbackofficestore";
            public const string LandingBackOfficeKey = "landing-backoffice-keys";
            public const string LandingBOIndex = "seasonal-pic-classified-index";
            public const string LandingServiceBOIndex = "seasonal-pic-Services-index";
            public const string FeaturedStoreClassifiedsIndexKey = "featured-store-classifieds-index";
            public const string FeaturedStoreServicesIndexKey = "featured-store-services-index";
            public const string FeaturedCategoryClassifiedIndex = "featured-category-classified-index";
            public const string FeaturedCategoryServiceIndex = "featured-category-services-index";
            public const string UnifiedStore = "adstore";
            public const string CommonStore = "commonstore";
            public const string UnifiedIndexKey = "ad-index";
            public const string ItemsIndexKey = "items-ad-index";
            public const string PrelovedIndexKey = "preloved-index";
            public const string CollectiblesIndexKey = "collectibles-index";
            public const string DealsIndexKey = "deals-index";
            public const string ItemsCategoryIndexKey = "items-category-index";
            public const string PrelovedCategoryIndexKey = "preloved-category-index";
            public const string CollectiblesCategoryIndexKey = "collectibles-category-index";
            public const string DealsCategoryIndexKey = "deals-category-index";
            public const string SubscriptionStores = "subscriptionstores";
            public const string SubscriptionStoresIndexKey= "subscription-stores-index";
        }
        public static class PubSubTopics
        {
            public const string IndexUpdates = "index-updates";
        }
        public static class Verticals
        {
            public const string Classifieds = "classifieds";
            public const string Services = "services";
        }
        public static class V2Content
        {
            public const string ContentStoreName = "contentstatestore";
            public const string ContentServiceAppId = "qln-content-ms";
            public const string NewsCommentPrefix = "news-comment";
            public const string NewsCommentIndexPrefix = "news-comment-index-";
            public const string NewsIndexKey = "news-index";
            public const string NewsCategoryIndexKey = "newscategory-index";
            public const string EventIndexKey = "event-index";
            public const string EventCategoryIndexKey = "event-category-index";
            public const string ReportsIndexKey = "report-category-index";
            public const string ReportsCommunityIndexKey = "reportcommunitypost-category-index";
            public const string ReportsCommunityCommentsIndexKey = "reportcommunitycomments-category-index";
            public const string ReportsArticleCommentsIndexKey = "reportarticlecomments-category-index";
            public const string DailyTopBOIndexKey = "daily-top-bo-index";
            public const string DailyTopicIndexKey = "daily-topic-index";
            public const string BannerTypeIndexKey = "banner-type-index";
            public const string BannerIndexKey = "banner-index";

        }
        public static class V2ClassifiedBo
        {
            public const string ClassifiedBoStoreName = "contentstatestore";
            public const string ClassifiedBoServiceAppId = "qln-classifiedBo-ms";

        }
        public static class Services
        {
            public const string StoreName = "servicestatestore";
            public const string IndexKey = "service-category-index";
            public const string ServiceAppId = "qln-classified-ms";
            public const string ServicesIndexKey = "services-index";
        }
        public static class Company
        {
            public const string CompanyStoreName = "companystatestore";
            public const string CompanyIndexKey = "company-index";
            public const string CompanyServiceAppId = "qln-company-ms";
        }
        public static class Audit
        {
            public const string StoreName = "servicestatestore";
            public const string AuditIndexKey = "audit_index";
        }
    }
}




