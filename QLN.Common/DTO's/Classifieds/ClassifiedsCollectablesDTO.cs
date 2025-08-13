using QLN.Common.DTO_s.Classified;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Classifieds
{
    public class ClassifiedsCollectablesDTO : ClassifiedsBaseDTO
    {
        public bool HasAuthenticityCertificate { get; set; }
        public string? AuthenticityCertificateName { get; set; }
        public string? AuthenticityCertificateUrl { get; set; }
        public bool HasWarranty { get; set; }
        public bool IsHandmade { get; set; }
        public string? YearOrEra { get; set; }
    }

}
