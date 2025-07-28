using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsPreloved : ClassifiedsBase
    {
        public bool? HasAuthenticityCertificate { get; set; }
        public string? AuthenticityCertificateUrl { get; set; }
        public string? Inclusion { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public bool IsRefreshed { get; set; }
        public DateTime? RefreshExpiryDate { get; set; }
    }

}
