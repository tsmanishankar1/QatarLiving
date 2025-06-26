using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.ClassifiedsIndex;

namespace QLN.Common.DTO_s
{
    public class ItemAdDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string SubVertical { get; set; }        
        public string Description { get; set; }
        public string Category { get; set; }
        public string? L1Category { get; set; }
        public string? L2Category { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public decimal Price { get; set; }
        public string? PriceType { get; set; }
        public string Condition { get; set; }
        public string Color { get; set; }
        public string AcceptsOffers { get; set; }
        public string? MakeType { get; set; }
        public string Capacity { get; set; }
        public string Processor { get; set; }
        public string Coverage { get; set; }
        public string Ram { get; set; }
        public string Resolution { get; set; }
        public string BatteryPercentage { get; set; }
        public string? Size { get; set; }
        public string? SizeValue { get; set; }
        public string? Gender { get; set; }
        public List<ImageInfo> ImageUrls { get; set; }
        public string CertificateUrl { get; set; }
        public string Phone { get; set; }
        public string WhatsAppNumber { get; set; }
        public string? Zone { get; set; }
        public string? StreetName { get; set; }
        public string? BuildingNumber { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public Guid UserId { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime CreatedAt { get; set; }
        public AdStatus Status { get; set; }
    }

    public class PaginatedAdResponseDto
    {
        public int Total { get; set; }
        public List<ItemAdDto> Items { get; set; } = new();
    }
}
