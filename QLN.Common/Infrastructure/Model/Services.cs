using QLN.Common.DTO_s;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace QLN.Common.Infrastructure.Model
{
    public class Services
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public ServicesCategory Category { get; set; } = default!;

        [Required]
        public Guid L1CategoryId { get; set; }

        [ForeignKey(nameof(L1CategoryId))]
        public L1Category L1Category { get; set; } = default!;

        [Required]
        public Guid L2CategoryId { get; set; }

        [ForeignKey(nameof(L2CategoryId))]
        public L2Category L2Category { get; set; } = default!;

        public string? CategoryName { get; set; }
        public string? L1CategoryName { get; set; }
        public string? L2CategoryName { get; set; }

        public bool IsPriceOnRequest { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string PhoneNumberCountryCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string WhatsappNumberCountryCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
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

        [Column(TypeName = "decimal(9,6)")]
        public decimal Longitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal Lattitude { get; set; }

        [Column(TypeName = "jsonb")]
        public List<ImageDto> PhotoUpload { get; set; } = new();

        public string? UserName { get; set; }

        public ServiceStatus? Status { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsPromoted { get; set; }

        public DateTime? PromotedExpiryDate { get; set; }
        public DateTime? FeaturedExpiryDate { get; set; }
        public DateTime? LastRefreshedOn { get; set; }

        public ServiceAdType AdType { get; set; }

        public DateTime? PublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }


}
