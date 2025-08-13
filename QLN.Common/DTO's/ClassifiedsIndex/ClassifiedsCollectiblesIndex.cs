using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsCollectiblesIndex : ClassifiedsIndexBase
    {
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsFeatured { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? FeaturedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsPromoted { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PromotedExpiryDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool HasAuthenticityCertificate { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? AuthenticityCertificateName { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? AuthenticityCertificateUrl { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool HasWarranty { get; set; }

        [SimpleField(IsFilterable = true)]
        public bool IsHandmade { get; set; }
        [SearchableField(IsFilterable = true, IsSortable = true)]
        public string? YearOrEra { get; set; }

    }
}
