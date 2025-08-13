using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsIndexBase
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? SubscriptionId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string SubVertical { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Slug { get; set; }

        [SearchableField(IsFilterable = true)]
        public string AdType { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Title { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Description { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public double? Price { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? PriceType { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? CategoryId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Category { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? L1CategoryId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? L1Category { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? L2CategoryId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? L2Category { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Location { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Brand { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Model { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Condition { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Color { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PublishedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? ExpiryDate { get; set; }

        [SearchableField(IsFilterable = true)]
        public string Status { get; set; }

        [SearchableField(IsFilterable = true)]
        public string UserId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string UserName { get; set; }

        [SimpleField(IsFilterable = true)]
        public double? Latitude { get; set; }

        [SimpleField(IsFilterable = true)]
        public double? Longitude { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? ContactNumberCountryCode { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? ContactNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? WhatsappNumberCountryCode { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? WhatsAppNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? ContactEmail { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? StreetNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? BuildingNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Zone { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool IsActive { get; set; }

        [SearchableField(IsFilterable = true)]
        public string CreatedBy { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime CreatedAt { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? UpdatedBy { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? UpdatedAt { get; set; }

        public IList<ImageInfo> Images { get; set; } = new List<ImageInfo>();

        [SearchableField(IsFilterable = true)]
        public string? AttributesJson { get; set; }
    }

    public class ImageInfo
    {
        [SimpleField(IsFilterable = true)]
        public string Url { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true)]
        public int Order { get; set; }
    }

    public class AttributeData
    {
        public Dictionary<string, string> Values { get; set; } = new();
    }

    public enum AdTypeEnum
    {
        P2P,
        Subscription,
        Free
    }
}
