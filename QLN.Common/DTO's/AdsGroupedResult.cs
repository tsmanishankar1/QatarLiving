using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class AdsGroupedResult
    {
        public List<ItemAd> PublishedAds { get; set; } = new();
        public List<ItemAd> UnpublishedAds { get; set; } = new();
    }
    public class AdsGroupedPrelovedResult
    {
        public List<PrelovedAd> PublishedAds { get; set; } = new();
        public List<PrelovedAd> UnpublishedAds { get; set; } = new();
    }

    public class ItemAdsAndDashboardResponse
    {        
        public ItemDashboardDto ItemsDashboard { get; set; }
        public AdsGroupedResult ItemsAds { get; set; }
    }

    public class PrelovedAdsAndDashboardResponse
    {        
        public PrelovedDashboardDto PrelovedDashboard { get; set; }
        public AdsGroupedPrelovedResult PrelovedAds { get; set; }

    }


}
