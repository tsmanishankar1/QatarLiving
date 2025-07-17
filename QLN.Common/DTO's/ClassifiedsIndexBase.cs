using Azure.Search.Documents.Indexes;
using Microsoft.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsIndexBase
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        [SearchableField(IsFilterable = true)]
        public string SubVertical { get; set; }

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
        public string? Category { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? L1Category { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? L2Category { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Location { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime CreatedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PublishedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? ExpiryDate { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Status { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? UserId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? UserName { get; set; }

        [SimpleField(IsFilterable = true)]
        public GeographyPoint? GeoLocation { get; set; }

        public IList<ImageInfo>? Images { get; set; }

        public Dictionary<string, string>? Attributes { get; set; }

    }
    public class ImageInfo
    {
        [SearchableField(IsFilterable = true)]
        public string Url { get; set; }

        [SimpleField(IsFilterable = true)]
        public int Order { get; set; }
    }
    public enum AdTypeEnum
    {
        P2P,
        Subscription,
        Free
    }
}
