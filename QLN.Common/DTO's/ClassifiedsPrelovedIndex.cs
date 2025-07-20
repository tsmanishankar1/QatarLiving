using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsPrelovedIndex : ClassifiedsIndexBase
    {
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsFeatured { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime FeaturedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsPromoted { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime PromotedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsRefreshed { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime RefreshExpiryDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool? HasAuthenticityCertificate { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? AuthenticityCertificateUrl { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? Inclusion { get; set; }
    }
}
