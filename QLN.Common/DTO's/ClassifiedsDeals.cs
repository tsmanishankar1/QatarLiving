using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsDeals
    {
        public Guid Id { get; set; } = new Guid();
        public string? BusinessName { get; set; }
        public string? BranchNames { get; set; }
        public string? BusinessType { get; set; }
        public string? DealTitle { get; set; }
        public string? DealDescription { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? FileUrl { get; set; }
        public string? DataFeedUrl { get; set; }
        public string? ContactNumber { get; set; }
        public string? WhatsappNumber { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? SocialMediaLinks { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
    }

}
