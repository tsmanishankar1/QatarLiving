
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
        public const string ClassifiedServiceApp = "qln-classified-ms";
        public const string SearchServiceApp = "qln-search-ms";
        public const string DocTypeStore = "Store";
        public const string DocTypeCategory = "Category";
        public const string DocTypeAd = "Ad";
        public const string DocTypeBanner = "Banner";
        //Index constants
        public const string backofficemaster = "backofficemaster";
        public static class EntityTypes
        {
            public const string HeroBanner = "HeroBanner";
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
            public const string CallToAction = "CallToAction";

        }
        public static class Verticals
        {
            public const string Classifieds = "classifieds";
            public const string Services = "Services";
        }
    }
}
