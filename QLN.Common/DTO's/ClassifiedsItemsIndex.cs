using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsItemsIndex : ClassifiedsIndexBase
    {
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsFeatured { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? FeaturedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsPromoted { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PromotedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsRefreshed { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? RefreshExpiryDate { get; set; }
    }
}
