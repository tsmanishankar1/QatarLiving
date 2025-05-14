using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
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
    public class ClassifiedIndex
    {       
        public string Id { get; set; } = Guid.NewGuid().ToString();       
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsFeaturedItem { get; set; }
        public bool? IsFeaturedCategory { get; set; }
        public bool? IsFeaturedStore { get; set; }
        public string Category { get; set; } = string.Empty;
        public string L1Category { get; set; } = string.Empty;        
        public string L2Category { get; set; } = string.Empty;     
        public string StoreName { get; set; }        
        public string StoreLogoUrl { get; set; }        
        public double Price { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string Colour { get; set; } = string.Empty;        
        public string Coverage { get; set; } = string.Empty;        
        public string Size { get; set; } = string.Empty;        
        public string Gender { get; set; } = string.Empty;        
        public DateTime CreatedDate { get; set; }
    }
}
