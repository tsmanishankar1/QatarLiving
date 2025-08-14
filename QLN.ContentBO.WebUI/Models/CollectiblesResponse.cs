namespace QLN.ContentBO.WebUI.Models
{
    public class CollectibleResponse
    {
        public int TotalCount { get; set; }
        public List<CollectibleItem> ClassifiedsCollectibles { get; set; } = [];
    }

    public class CollectibleItem
    {
        public DateTime? FeaturedExpiryDate { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        public bool HasAuthenticityCertificate { get; set; }
        public string? AuthenticityCertificateName { get; set; }
        public string? AuthenticityCertificateUrl { get; set; }
        public bool HasWarranty { get; set; }
        public bool IsHandmade { get; set; }
        public string? YearOrEra { get; set; }
        public long Id { get; set; }
        public int SubVertical { get; set; }
        public int AdType { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? PriceType { get; set; }
        public long? CategoryId { get; set; }
        public string? Category { get; set; }
        public long? L1CategoryId { get; set; }
        public string? L1Category { get; set; }
        public long? L2CategoryId { get; set; }
        public string? L2Category { get; set; }
        public string? Location { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Condition { get; set; }
        public string? Color { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int Status { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ContactNumberCountryCode { get; set; }
        public string? ContactNumber { get; set; }
        public string? ContactEmail { get; set; }
        public string? WhatsappNumberCountryCode { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public string? Zone { get; set; }
        public List<CollectibleImage> Images { get; set; } = [];
        public object? Attributes { get; set; } // Could be a more specific type if known
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? SubscriptionId { get; set; }
    }

    /// <summary>
    /// Represents an image associated with a classified.
    /// </summary>
    public class CollectibleImage
    {
        public string? AdImageFileNames { get; set; }
        public string? Url { get; set; }
        public int Order { get; set; }
    }
}
