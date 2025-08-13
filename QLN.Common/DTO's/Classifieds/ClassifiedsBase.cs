using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsBase
    {
        public long Id { get; set; } 
        public SubVertical SubVertical { get; set; } 
        public AdTypeEnum AdType { get; set; }
        public string Title { get; set; } = string.Empty;
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
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Condition { get; set; }
        public string? Color { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public AdStatus Status { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string ContactNumberCountryCode { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;
        public string WhatsAppNumber { get; set; } = string.Empty;
        public string? StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public string zone { get; set; }
        public List<ImageInfo> Images { get; set; } = new List<ImageInfo>();
        public Dictionary<string, string>? Attributes { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? SubscriptionId { get; set; }
    }

}
