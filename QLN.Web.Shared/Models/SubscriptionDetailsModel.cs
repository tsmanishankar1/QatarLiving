using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Models
{
    public class SubscriptionDetailsModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public BusinessProfile BusinessProfile { get; set; }
        public SubscriptionStatistics SubscriptionStatistics { get; set; }
    }

    public class BusinessProfile
    {
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Duration { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string LogoUrl { get; set; }
    }

    public class SubscriptionStatistics
    {
        public UsageItem PublishedAds { get; set; }
        public UsageItem PromotedAds { get; set; }
        public UsageItem FeaturedAds { get; set; }
        public UsageItem Refreshes { get; set; }
    }

    public class UsageItem
    {
        public int Usage { get; set; }
        public int Total { get; set; }
    }

}
