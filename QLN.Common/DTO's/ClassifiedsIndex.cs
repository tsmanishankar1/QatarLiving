using Azure.Core.Serialization;
using Azure.Search.Documents.Indexes;
using Microsoft.Spatial;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsIndex
    {
        // Primary key
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // --- COMMON AD FIELDS ---
        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? SubVertical { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? SubscriptionId { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Title { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Description { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool? IsFeatured { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? FeatureExpiryDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool? IsPromoted { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PromotedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool? IsRefreshed { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? RefreshExpiryDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? AdType { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? Status { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? Price { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? PriceType { get; set; }

        // --- CATEGORIES ---
        [SimpleField(IsFilterable = true)]
        public string? CategoryId { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Category { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? L1Category { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? L2Category { get; set; }

        // --- LOCATION & CONTACT ---
        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Location { get; set; }

        [JsonConverter(typeof(MicrosoftSpatialGeoJsonConverter))]
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public GeographyPoint? GeoLocation { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? StreetNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? BuildingNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Zone { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? PhoneNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? WhatsappNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? ContactEmail { get; set; }

        // --- DATES ---
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? CreatedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? ModifiedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? ExpiryDate { get; set; }


        // --- ITEM-AD SPECIFIC ---

        [SearchableField(IsFilterable = true)]
        public string? Brand { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Make { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Model { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Capacity { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Storage { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Colour { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Coverage { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? SizeType { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Size { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Gender { get; set; }

        [SimpleField(IsFilterable = true)]
        public int? BatteryPercentage { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? HasWarrantyCertificate { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? WarrantyCertificateUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Processor { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Ram { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Resolution { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? FlyerFileName { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? FlyerFileUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? FlyerXmlLink { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? FlyerCoverImageUrl { get; set; }

        // --- COLLECTIBLE-ITEM SPECIFIC ---

        [SimpleField(IsFilterable = true)]
        public int? Theme { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? HasAuthenticityCertificate { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? AuthenticityUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? YearEra { get; set; }

        [SimpleField(IsFilterable = true)]
        public int? CountryOfOrigin { get; set; }

        [SimpleField(IsFilterable = true)]
        public int? Language { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? IsGraded { get; set; }

        [SimpleField(IsFilterable = true)]
        public int? GradingCompany { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Grade { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Rarity { get; set; }

        [SimpleField(IsFilterable = true)]
        public int? Package { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Material { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? SerialNumber { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? IsSigned { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? SignedBy { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? IsFramed { get; set; }

        // --- PRE-LOVED-AD SPECIFIC ---
        [SearchableField(IsFilterable = true)]
        public string? Inclusion { get; set; }
        public IList<ImageInfo>? Images { get; set; }

        // --- STORE --
        [SimpleField(IsFilterable = true)]
        public string? StoreId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? StoreName { get; set; }

        [SimpleField(IsFilterable = true)]
        public string StoreLogoUrl { get; set; } = string.Empty;

        // --- PUBLISHER DETAILS ---
        [SimpleField(IsFilterable = true)]
        public string? UserId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? UserName { get; set; }

        public class ImageInfo
        {
            public string AdImageFileNames { get; set; }

            [SearchableField(IsFilterable = true)]
            public string Url { get; set; } = string.Empty;

            [SimpleField(IsFilterable = true)]
            public int Order { get; set; }
        }
    }
}
