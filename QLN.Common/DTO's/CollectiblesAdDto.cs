using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class CollectiblesAdDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SubVertical { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string? L2Category { get; set; }
        public bool HasAuthenticityCertificate { get; set; }
        public string AuthenticityCertificateUrl { get; set; }
        public string? YearOrEra { get; set; }
        public string Condition { get; set; }
        public string? Brand { get; set; }
        public string? CountryOfOrigin { get; set; }
        public string? Language { get; set; }
        public string? Rarity { get; set; }
        public string? Package { get; set; }
        public bool? IsGraded { get; set; }
        public string? GradingCompany { get; set; }
        public string? Grades { get; set; }
        public string? Material { get; set; }
        public string? Scale { get; set; }
        public string SerialNumber { get; set; }
        public bool? Signed { get; set; }
        public string? SignedBy { get; set; }
        public string? FramedBy { get; set; }
        public decimal Price { get; set; }
        public string PriceType { get; set; }                    
        public List<string> ImageUrls { get; set; } = new();
        public string PhoneNumber { get; set; }
        public string WhatsAppNumber { get; set; }
        public string ContactEmail { get; set; }
        public List<string> Location { get; set; }
        public string StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public bool HasWarranty { get; set; }
        public bool IsHandmade { get; set; }                         
        public bool TearmsAndCondition { get; set; }
        public Guid UserId { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime CreatedAt { get; set; }
        public AdStatus Status { get; set; }

    }

    public class CollectiblesAdListDto
    {
        public List<CollectiblesAdDto> PublishedAds { get; set; } = new();
        public List<CollectiblesAdDto> UnpublishedAds { get; set; } = new();
    }
}
