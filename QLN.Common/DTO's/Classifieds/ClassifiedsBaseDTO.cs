using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Classified
{
    public class ClassifiedsBaseDTO
    {
        public AdTypeEnum AdType { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public string? PriceType { get; set; }
        public string? CategoryId { get; set; }
        public string? Category { get; set; }
        public string? L1CategoryId { get; set; }
        public string? L1Category { get; set; }
        public string? L2CategoryId { get; set; }
        public string? L2Category { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string ContactNumber { get; set; }
        public string ContactEmail { get; set; }
        public string WhatsAppNumber { get; set; }
        public string? StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public string? zone { get; set; }
        public List<ImageInfo> Images { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
       
    }

}
