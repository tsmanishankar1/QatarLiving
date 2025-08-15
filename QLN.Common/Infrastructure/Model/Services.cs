using QLN.Common.DTO_s;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLN.Common.Infrastructure.Model
{
    public class Services
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long CategoryId { get; set; }

        [Required]
        public long L1CategoryId { get; set; }

        [Required]
        public long L2CategoryId { get; set; }
        [MaxLength(100)]
        public string? CategoryName { get; set; }

        [MaxLength(100)]
        public string? L1CategoryName { get; set; }

        [MaxLength(100)]
        public string? L2CategoryName { get; set; }

        public bool IsPriceOnRequest { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = default!;

        [Required]
        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string PhoneNumberCountryCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string WhatsappNumber { get; set; } = string.Empty;

        [EmailAddress]
        public string? EmailAddress { get; set; }

        [Required]
        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        public int? LocationId { get; set; }
        [MaxLength(50)]
        public Guid? SubscriptionId { get; set; }
        [Required]
        [MaxLength(50)]
        public string ZoneId { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? StreetNumber { get; set; }
        [MaxLength(20)]
        public string? BuildingNumber { get; set; }
        [MaxLength(150)]
        public string? LicenseCertificate { get; set; }
        [MaxLength(255)]
        public string? Comments { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal Longitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal Lattitude { get; set; }

        [Column(TypeName = "jsonb")]
        public List<ImageDto> PhotoUpload { get; set; } = new();
        [MaxLength(100)]
        public string? UserName { get; set; }
        public ServiceStatus? Status { get; set; }
        public bool IsRefreshed { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime? PromotedExpiryDate { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public DateTime? LastRefreshedOn { get; set; }
        public ServiceAdType AdType { get; set; }
        [MaxLength(50)]
        public string? Availability { get; set; }
        [MaxLength(50)]
        public string? Duration { get; set; }
        [MaxLength(100)]
        public string? Reservation { get; set; }
        [MaxLength(200)]
        public string? Slug { get; set; }
        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
