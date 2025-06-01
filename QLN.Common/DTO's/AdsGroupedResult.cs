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
}
