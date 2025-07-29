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
        public bool IsFeatured { get; set; } = false;
        public DateTime? FeaturedExpiryDate { get; set; } = null;
        public bool IsPromoted { get; set; } = false;
        public DateTime? PromotedExpiryDate { get; set; } = null;
        public bool IsRefreshed { get; set; }= false;
        public DateTime? RefreshExpiryDate { get; set; } = null;
    }

}
