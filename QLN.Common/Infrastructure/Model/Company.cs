using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class Company
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid CategoryId { get; set; }
        [Required]
        public Guid L1CategoryId { get; set; }
        [Required]
        public Guid L2CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? L1CategoryName { get; set; }
        public string? L2CategoryName { get; set; }
        public bool IsPriceOnRequest { get; set; }
        public decimal? Price { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public string PhoneNumberCountryCode { get; set; } = string.Empty;
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;
        [Required]
        public string WhatsappNumber { get; set; } = string.Empty;
        [EmailAddress]
        public string? EmailAddress { get; set; }
        [Required]
        public string Location { get; set; } = string.Empty;
        public int? LocationId { get; set; }
        public string? SubscriptionId { get; set; }
        [Required]
        public string ZoneId { get; set; } = string.Empty;
        public string? StreetNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public string? LicenseCertificate { get; set; }
        public string? Comments { get; set; }
        public decimal Longitude { get; set; }
        public decimal Lattitude { get; set; }
        public List<ImageDto>? PhotoUpload { get; set; }
        public string? UserName { get; set; }
        public ServiceStatus? Status { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        public bool IsRefreshed => LastRefreshedOn.HasValue && LastRefreshedOn.Value > DateTime.UtcNow;
        public DateTime? PromotedExpiryDate { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public DateTime? LastRefreshedOn { get; set; }
        public ServiceAdType AdType { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
