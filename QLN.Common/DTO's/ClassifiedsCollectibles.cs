using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsCollectibles : ClassifiedsBase
    {
        public bool IsFeatured { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public bool HasAuthenticityCertificate { get; set; }
        public string? AuthenticityCertificateUrl { get; set; }
        public bool HasWarranty { get; set; }
        public bool IsHandmade { get; set; }
        public string? YearOrEra { get; set; }
    }

}
