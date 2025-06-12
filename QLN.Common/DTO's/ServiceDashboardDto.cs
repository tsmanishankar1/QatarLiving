using Google.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ServiceDashboardDto
    {
        public int PublishedAds { get; set; }
        public int PromotedAds { get; set; }
        public int FeaturedAds { get; set; }
        public int Refreshes { get; set; }
        public int RemainingRefreshes { get; set; }
        public int TotalAllowedRefreshes { get; set; }
        public DateTime? RefreshExpiry { get; set; }
        public int Impressions { get; set; }
        public int Views { get; set; }
        public int WhatsAppClicks { get; set; }
        public int Calls { get; set; }
    }


    public class ServiceDashboardWithAdsDto
    {
        public ServiceDashboardDto Dashboard { get; set; } = new();
        public List<ServiceAd> PublishedAds { get; set; } = new();
        public List<ServiceAd> UnpublishedAds { get; set; } = new();
    }
}
