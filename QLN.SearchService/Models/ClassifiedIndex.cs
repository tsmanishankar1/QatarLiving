using Azure.Search.Documents.Indexes;
using System.Text.Json.Serialization;

namespace QLN.SearchService.IndexModels
{
    public class ClassifiedIndex
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Title { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string? DocType { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? SubVertical { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Description { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public bool? IsFeaturedItem { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? IsFeaturedCategory { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? IsFeaturedStore { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Category { get; set; } = string.Empty;

        [SimpleField(IsFilterable = false)]
        public string? CategoryImageUrl { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Brand { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? L1Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Capacity { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? L2Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Location { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public string? BannerTitle { get; set; }

        [SimpleField(IsFilterable = false)]
        public string? BannerImageUrl { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? StoreName { get; set; }

        [SimpleField(IsFilterable = false)]
        public string? StoreLogoUrl { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? Price { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Zone { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? StreetNumber { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? BuildingNumber { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Make { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Model { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Condition { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Storage { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Colour { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Coverage { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? SizeType { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Size { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Gender { get; set; } = string.Empty;

        [SimpleField(IsSortable = true)]
        public DateTime CreatedDate { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? FlyerFileName { get; set; }

        [SimpleField(IsFilterable = false)]
        public string? FlyerCoverImageUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? FlyerXmlLink { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? ExpiryDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public int? BatteryPercentage { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? HasWarrantyCertificate { get; set; }

        [SimpleField(IsFilterable = false)]
        public string? WarrantyCertificateUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Processor { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Ram { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? PhoneNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? WhatsappNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Resolution { get; set; }

        [SimpleField(IsFilterable = false, IsFacetable = false)]
        public List<string>? ImageUrls { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string UserId { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsPublished { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Impressions { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Views { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Calls { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long WhatsAppClicks { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Shares { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Saves { get; set; }
    }
}
