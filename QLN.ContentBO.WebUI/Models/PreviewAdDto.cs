namespace QLN.ContentBO.WebUI.Models
{
    public interface PreviewAdDto
    {
        string? CategoryId { get; set; }
        string? L1CategoryId { get; set; }
        string? L2CategoryId { get; set; }

        Dictionary<string, string> DynamicFields { get; set; }

        string? Title { get; set; }
        string? Description { get; set; }
        string? XmlLink { get; set; }
        string? FlyerLocation { get; set; }
        int Price { get; set; }

        string? ContactNumberCountryCode { get; set; }
        string? ContactNumber { get; set; }
        string? WhatsappNumberCountryCode { get; set; }
        string? WhatsappNumber { get; set; }

        string? Zone { get; set; }
        string? StreetNumber { get; set; }
        string? BuildingNumber { get; set; }
        double? Latitude { get; set; }
        double? Longitude { get; set; }

        bool IsAgreed { get; set; }

        List<AdImage> Images { get; set; }

        string? Id { get; set; }
        string? UserId { get; set; }
        string? UserName { get; set; }
        string? ContactEmail { get; set; }

        int? Status { get; set; }
        bool IsFeatured { get; set; }
        bool IsPromoted { get; set; }
        bool IsRefreshed { get; set; }
        DateTime? RefreshExpiryDate { get; set; }
        DateTime? FeaturedExpiryDate { get; set; }
        DateTime? PromotedExpiryDate { get; set; }

        string? Location { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
