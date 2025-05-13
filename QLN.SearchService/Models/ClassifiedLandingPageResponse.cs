using QLN.SearchService.IndexModels;

namespace QLN.SearchService.Models
{
    public class ClassifiedLandingPageResponse
    {
        public IEnumerable<ClassifiedIndex> FeaturedItems { get; set; }
        public IEnumerable<LandingCategoryInfo> FeaturedCategories { get; set; } 
        public IEnumerable<LandingStoreInfo> FeaturedStores { get; set; }        
        public IEnumerable<CategoryAdCount> CategoryAdCounts { get; set; }       
    }

    public class LandingCategoryInfo
    {
        public string Category { get; set; }
        public string ImageUrl { get; set; }
    }

    public class LandingStoreInfo
    {
        public string StoreName { get; set; }
        public string LogoUrl { get; set; }
        public int ItemCount { get; set; }
    }

    public class CategoryAdCount
    {
        public string Category { get; set; }
        public int Count { get; set; }
    }


}
