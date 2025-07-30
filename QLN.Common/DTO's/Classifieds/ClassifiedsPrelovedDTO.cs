using QLN.Common.DTO_s.Classified;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Classifieds
{
    public class ClassifiedsPrelovedDTO : ClassifiedsBaseDTO
    {
        
        public string? Inclusion { get; set; }
        public bool? HasAuthenticityCertificate { get; set; }
        public string? AuthenticityCertificateUrl { get; set; }

    }

}
