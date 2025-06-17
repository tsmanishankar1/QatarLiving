using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class LandingPageDto
    {
        public IEnumerable<LandingBackOfficeIndex>? HeroBanner { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? TakeOverBanner { get; set; }
        public IEnumerable<LandingFeaturedItemDto>? FeaturedItems { get; set; }
        public IEnumerable<LandingFeaturedItemDto>? FeaturedServices { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? FeaturedCategories { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? ReadyToGrow { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? FeaturedStores { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? Categories { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? SeasonalPicks { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? SocialPostDetail { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? SocialLinks { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? SocialMediaVideos { get; set; }
        public IEnumerable<LandingBackOfficeIndex>? FaqItems { get; set; }
        public IEnumerable<PopularSearchDto>? PopularSearches { get; set; }

    }
}
