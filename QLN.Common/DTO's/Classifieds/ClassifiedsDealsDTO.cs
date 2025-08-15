using QLN.Common.DTO_s.Classified;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsDealsDTO
    {               
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? FlyerFileUrl { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? DataFeedUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string XMLlink { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? Offertitle { get; set; }
        public string? Description { get; set; }
        public string CoverImage { get; set; }
        public LocationsDtos Locations { get; set; }
    }

}
