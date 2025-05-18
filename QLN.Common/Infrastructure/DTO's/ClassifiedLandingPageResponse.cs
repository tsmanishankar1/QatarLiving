using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class ClassifiedLandingPageResponse
    {
        public IEnumerable<ClassifiedIndexDto> FeaturedItems { get; set; } = Enumerable.Empty<ClassifiedIndexDto>();
        public IEnumerable<LandingCategoryInfo> FeaturedCategories { get; set; } = Enumerable.Empty<LandingCategoryInfo>();
        public IEnumerable<LandingStoreInfo> FeaturedStores { get; set; } = Enumerable.Empty<LandingStoreInfo>();
        public IEnumerable<CategoryAdCount> CategoryAdCounts { get; set; } = Enumerable.Empty<CategoryAdCount>();
    }

    public class ClassifiedIndexDto
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public bool IsFeaturedItem { get; set; }
        public bool IsFeaturedCategory { get; set; }
        public bool IsFeaturedStore { get; set; }

        public string Category { get; set; } = "";
        public string CategoryImageUrl { get; set; } = "";
        public string Subcategory { get; set; } = "";
        public string Brand { get; set; } = "";

        public string L1Category { get; set; } = "";
        public string Capacity { get; set; } = "";
        public string L2Category { get; set; } = "";

        public string Location { get; set; } = "";
        public string Zone { get; set; } = "";
        public string StreetNumber { get; set; } = "";
        public string BuildingNumber { get; set; } = "";

        public string StoreName { get; set; } = "";
        public string StoreLogoUrl { get; set; } = "";

        public double? Price { get; set; }

        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public string Condition { get; set; } = "";
        public string Storage { get; set; } = "";
        public string Colour { get; set; } = "";
        public string Coverage { get; set; } = "";

        public string SizeType { get; set; } = "";
        public string Size { get; set; } = "";
        public string Gender { get; set; } = "";

        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public string FlyerFileName { get; set; } = "";
        public string FlyerCoverImageUrl { get; set; } = "";
        public string FlyerXmlLink { get; set; } = "";

        public int? BatteryPercentage { get; set; }
        public bool? HasWarrantyCertificate { get; set; }
        public string WarrantyCertificateUrl { get; set; } = "";

        public string Processor { get; set; } = "";
        public string Ram { get; set; } = "";

        public string PhoneNumber { get; set; } = "";
        public string WhatsappNumber { get; set; } = "";
        public string Resolution { get; set; } = "";

        public IList<string> ImageUrls { get; set; } = new List<string>();

        public string UserId { get; set; } = "";
        public bool IsPublished { get; set; }
        public long Impressions { get; set; }
        public long Views { get; set; }
        public long Calls { get; set; }
        public long WhatsAppClicks { get; set; }
        public long Shares { get; set; }
        public long Saves { get; set; }
    }

    public class LandingCategoryInfo
    {
        public string Category { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }

    public class LandingStoreInfo
    {
        public string StoreName { get; set; } = "";
        public string LogoUrl { get; set; } = "";
        public int ItemCount { get; set; }
    }

    public class CategoryAdCount
    {
        public string Category { get; set; } = "";
        public int Count { get; set; }
    }
}
