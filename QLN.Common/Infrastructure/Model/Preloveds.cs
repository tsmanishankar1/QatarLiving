using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class Preloveds : ClassifiedBase
    {
        public bool? HasAuthenticityCertificate { get; set; }
        public string? AuthenticityCertificateName { get; set; }
        public string? AuthenticityCertificateUrl { get; set; }
        public string? Inclusion { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; } = null;
        public DateTime? PromotedExpiryDate { get; set; } = null;
        public DateTime? LastRefreshedOn { get; set; } = null;
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        public bool IsRefreshed { get; set; }
    }
}
