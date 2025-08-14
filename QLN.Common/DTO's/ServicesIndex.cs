using Azure.Search.Documents.Indexes;

namespace QLN.Common.DTO_s
{
    public class ServicesIndex
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public string CategoryId { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public string L1CategoryId { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public string L2CategoryId { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? CategoryName { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? L1CategoryName { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? L2CategoryName { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public double? Price { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Title { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Description { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public string PhoneNumberCountryCode { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string PhoneNumber { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string WhatsappNumber { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string? EmailAddress { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Location { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public int? LocationId { get; set; }

        [SimpleField(IsFilterable = true)]
        public double Lattitude { get; set; }

        [SimpleField(IsFilterable = true)]
        public double Longitude { get; set; }
        public IList<ImageInfo>? Images { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? UserName { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string? Status { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsFeatured { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? FeaturedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsPromoted { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PromotedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string AdType { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PublishedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? ExpiryDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool IsActive { get; set; }

        [SearchableField(IsFilterable = true)]
        public string CreatedBy { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime CreatedAt { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? UpdatedBy { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? UpdatedAt { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsRefreshed { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? LastRefreshedOn { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsPriceOnRequest { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? SubscriptionId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string ZoneId { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string? StreetNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? BuildingNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? LicenseCertificate { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Comments { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Availability { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Duration { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Reservation { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Slug { get; set; }
    }

    public class ServiceImageInfo
    {
        [SearchableField(IsFilterable = true)]
        public string Url { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public int Order { get; set; }
    }
}
