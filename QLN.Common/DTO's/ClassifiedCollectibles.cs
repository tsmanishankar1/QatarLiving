using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedCollectibles : CommonAdBase
    {
        public Guid Id { get; set; }
        public string? YearOrEra { get; set; }
        public string? Rarity { get; set; }
        public string? Package { get; set; }
        public bool HasAuthenticityCertificate { get; set; }
        public bool? IsGraded { get; set; }
        public string? GradingCompany { get; set; }
        public string? Grades { get; set; }
        public string? Material { get; set; }
        public string? Scale { get; set; }
        public string SerialNumber { get; set; }
        public bool? Signed { get; set; }
        public string? SignedBy { get; set; }
        public string? FramedBy { get; set; }
        public bool HasWarranty { get; set; }
        public bool IsHandmade { get; set; }        
    }
}
