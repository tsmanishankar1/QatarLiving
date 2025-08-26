using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class PagedResult<T>
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<T> Items { get; set; } = new List<T>();
    }
    public class P2pListingModal
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AdTitle { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Section { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPromoted { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateExpiry { get; set; }
        public List<ImageUpload>? ImageUpload { get; set; }
        public string OrderId { get; set; } = string.Empty;
    }
    public class DealsListingModal
    {
        public string Id { get; set; }
        public long? AdId { get; set; }
        public string DealTitle { get; set; } = string.Empty;
        public List<string> Location { get; set; } = new List<string>();
        public string ContactNumber { get; set; } = string.Empty;
        public string WhatsappNumber { get; set; } = string.Empty;
        public string? WhatsAppLeads { get; set; } = string.Empty;
        public string? PhoneLeads { get; set; } = string.Empty;
        public string? WebUrl { get; set; }
        public string? SubscriptionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Price { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? CreatedBy { get; set; } = string.Empty;
        public string? UserName { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public List<ImageUpload>? ImageUpload { get; set; }
        public long? OrderId { get; set; }
        public int WebClick { get; set; }
        public int Views { get; set; }
        public int Impression { get; set; }
        public int PhoneLead { get; set; }
    }
    public class ImageUpload
    {
        public string Url { get; set; } = string.Empty;
        public int Order { get; set; }
    }
    public class DealsModal
    {
        public long Id { get; set; }
        public string Subvertical { get; set; }
        public string SubscriptionId { get; set; }
        public string UserId { get; set; }
        public string BusinessName { get; set; }
        public string BranchNames { get; set; }
        public string BusinessType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string FlyerFileName { get; set; }
        public string FlyerFileUrl { get; set; }
        public string DataFeedUrl { get; set; }
        public string ContactNumberCountryCode { get; set; }
        public string ContactNumber { get; set; }
        public string WhatsappNumberCountryCode { get; set; }
        public string WhatsappNumber { get; set; }
        public string WebsiteUrl { get; set; }
        public string SocialMediaLinks { get; set; }
        public bool IsActive { get; set; }
        public LocationsWrapper Locations { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string XmlLink { get; set; }
        public string OfferTitle { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        [Required(ErrorMessage = "You must agree to the terms.")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms.")]
        public bool IsAgreed { get; set; } = true;


        public List<AdImage> Images { get; set; } = new()
        {
            new AdImage { Order = 0 },
            new AdImage { Order = 1 },
            new AdImage { Order = 2 }
        };
    }
    public class LocationsWrapper
    {
        public List<string> Locations { get; set; } = new List<string>();
    }

}
