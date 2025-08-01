using QLN.Common.DTO_s.Classified;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsDealsDTO : ClassifiedsBaseDTO
    {
        
        public string? BusinessName { get; set; }
        public string? BranchNames { get; set; }
        public string? BusinessType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? FlyerFileUrl { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? DataFeedUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? SocialMediaLinks { get; set; }
        public string XMLlink { get; set; } = string.Empty;
        public string? offertitle { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public List<string> Locations { get; set; } = new List<string>();
    }

}
