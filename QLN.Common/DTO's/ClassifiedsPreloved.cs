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
    }

}
