using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsDeals
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Subvertical { get; set; } = string.Empty;
        public string? SubscriptionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? BusinessName { get; set; }
        public string? BranchNames { get; set; }
        public string? BusinessType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string FlyerFileUrl { get; set; } = string.Empty;
        public string? DataFeedUrl { get; set; }
        public string ContactNumberCountryCode { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;
        public string WhatsappNumber { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public string? SocialMediaLinks { get; set; }
        public bool IsActive { get; set; }
        public List<string> Locations {  get; set; } = new List<string>();
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string XMLlink { get; set; } = string.Empty;
        public string? offertitle { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; } = null;
        public DateTime? PromotedExpiryDate { get; set; } = null;
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }        
    }

}
