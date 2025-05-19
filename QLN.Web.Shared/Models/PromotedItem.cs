namespace QLN.Web.Shared.Models
{
    public class PromotedItem
    {
        public string Id { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string StreetNumber { get; set; } = string.Empty;
        public string BuildingNumber { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string Colour { get; set; } = string.Empty;

        public string RAM { get; set; } = string.Empty;
        public string Processor { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;

        // ✅ Nullable to handle nulls from JSON
        public int? BatteryPercentage { get; set; }

        public string Coverage { get; set; } = string.Empty;
        public string WarrantyCertificateUrl { get; set; } = string.Empty;

        public string SizeType { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;

        public string Company { get; set; } = string.Empty;
        public string CompanyLogoUrl { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
        public string WhatsAppNumber { get; set; } = string.Empty;

        public bool IsPromoted { get; set; }
        public bool IsFeatured { get; set; }

        public DateTime CreatedDate { get; set; }

        // ✅ Nullable to prevent crash on null
        public DateTime? ExpiryDate { get; set; }

        public string FlyerCoverImageUrl { get; set; } = string.Empty;
        public string FlyerXmlLink { get; set; } = string.Empty;
    }
}
