using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class LandingBackOfficeIndex
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; } = default!;

        [SearchableField(IsFilterable = true, IsSortable= true)]
        public string EntityType { get; set; } = default!;   

        [SearchableField(IsFilterable= true, IsSortable= true)]
        public string Vertical { get; set; } = default!;

        [SearchableField(IsFilterable = true, IsSortable = true)]
        public string? Subvertical { get; set; }

        [SearchableField(IsFilterable= true, IsSortable= true)]
        public string Title { get; set; } = default!;

        [SearchableField]
        public string? Description { get; set; }

        [SimpleField(IsFilterable= true, IsSortable= true)]
        public int Order { get; set; }

        [SimpleField(IsFilterable= true, IsSortable= true)]
        public string? ParentId { get; set; }

        [SimpleField(IsFilterable= true)]
        public bool IsActive { get; set; } = true;

        [SimpleField(IsFilterable= true)]
        public string? RediectUrl { get; set; }

        [SearchableField]
        public string? ImageUrl { get; set; }

        [SimpleField(IsFilterable= true)]
        public int? ListingCount { get; set; }

        [SimpleField(IsFilterable= true)]
        public int? RotationSeconds { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? EntityId { get; set; }

        [SearchableField]
        public string? PayloadJson { get; set; }
            // ✅ New property for UI only
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsExpanded { get; set; } = false;
    }
}
