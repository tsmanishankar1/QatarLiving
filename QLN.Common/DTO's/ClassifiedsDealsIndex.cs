using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsDealsIndex
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? BusinessName { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? BranchNames { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? BusinessType { get; set; } 

        [SearchableField(IsFilterable = true)]
        public string? DealTitle { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? DealDescription { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? StartDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? EndDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? FileUrl { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? DataFeedUrl { get; set; } 

        [SearchableField(IsFilterable = true)]
        public string? ContactNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? WhatsappNumber { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? WebsiteUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? SocialMediaLinks { get; set; }

        [SearchableField(IsFilterable = true)]
        public string CreatedBy { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime CreatedAt { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? UpdatedBy { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? UpdatedAt { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool IsActive { get; set; }
    }
}
