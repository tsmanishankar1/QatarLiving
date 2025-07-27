using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBoIndex
{
    public class FeaturedCategoryIndex
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; } = null!;

        [SearchableField(IsFilterable = true)]
        public string Category { get; set; } = null!;

        [SearchableField(IsFilterable = true)]
        public string CategoryId { get; set; } = null!;

        [SearchableField(IsFilterable = true)]
        public string Vertical { get; set; } = null!;

        [SimpleField]
        public string? UserId { get; set; }

        [SearchableField]
        public string? UserName { get; set; }

        [SimpleField]
        public bool? IsActive { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateOnly StartDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateOnly EndDate { get; set; }

        [SimpleField]
        public int? SlotOrder { get; set; }

        [SimpleField(IsFilterable = true)]
        public string ImageUrl { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? CreatedAt { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? UpdatedAt { get; set; }

    }
}
