using System;
using System.Collections.Generic;
using Azure.Search.Documents.Indexes;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Microsoft.Spatial;
using static QLN.Common.DTO_s.ClassifiedsIndex;

namespace QLN.Common.DTO_s
{
    public class ServicesIndex
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Title { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string? Section { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string? CategoryId { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Category { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? SubCategory { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? Price { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? AcceptOffers { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? LicenseDocument { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Description { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string? PhoneNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? WhatsappNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Email { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? UserId { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Zone { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? StreetNumber { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? BuildingNumber { get; set; } = string.Empty;

        [JsonConverter(typeof(MicrosoftSpatialGeoJsonConverter))]
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public GeographyPoint? GeoLocation { get; set; }

        [SimpleField(IsFilterable = false, IsFacetable = false)]
        public IList<ImageInfo>? Images { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? CreatedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? ModifiedDate { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string? Status { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool? IsFeatured { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool? IsPromoted { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? RefreshExpiry { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public int? RemainingRefreshes { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public int? TotalAllowedRefreshes { get; set; }
    }
}
