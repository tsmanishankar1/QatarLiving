using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class LandingPageDto
    {
        public IEnumerable<BackofficemasterIndex>? HeroBanner { get; set; }
        public IEnumerable<BackofficemasterIndex>? FeaturedItems { get; set; }
        public IEnumerable<BackofficemasterIndex>? FeaturedServices { get; set; }
        public IEnumerable<BackofficemasterIndex>? FeaturedCategories { get; set; }
        public IEnumerable<BackofficemasterIndex>? ReadyToGrow { get; set; }
        public IEnumerable<BackofficemasterIndex>? FeaturedStores { get; set; }
        public IEnumerable<BackofficemasterIndex>? Categories { get; set; }
        public IEnumerable<BackofficemasterIndex>? SeasonalPicks { get; set; }
        public IEnumerable<BackofficemasterIndex>? SocialPostDetail { get; set; }
        public IEnumerable<BackofficemasterIndex>? SocialLinks { get; set; }
        public IEnumerable<BackofficemasterIndex>? SocialMediaVideos { get; set; }
        public IEnumerable<BackofficemasterIndex>? FaqItems { get; set; }

    }
}
