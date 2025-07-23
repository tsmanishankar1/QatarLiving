using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedsBase
    {
        public Guid Id { get; set; } = new Guid();
        public string SubVertical { get; set; }
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
        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public AdStatus Status { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string ContactNumber { get; set; }
        public string ContactEmail { get; set; }
        public string WhatsAppNumber { get; set; }
        public string? StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public string? zone { get; set; }
        public List<ImageInfoDto> Images { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ImageInfoDto
    {
        public string AdImageFileNames { get; set; }
        public string Url { get; set; }
        public int Order { get; set; }
    }

}
