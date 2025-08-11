using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Company;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.Model
{
    public class Company
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, Phone, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string PhoneNumberCountryCode { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public List<string>? BranchLocations { get; set; }

        [Required, Phone, MaxLength(20)]
        public string WhatsAppNumber { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string WhatsAppCountryCode { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Url, MaxLength(100)]
        public string? WebsiteUrl { get; set; }

        [Url, MaxLength(100)]
        public string? FacebookUrl { get; set; }

        [Url, MaxLength(100)]
        public string? InstagramUrl { get; set; }

        [Required, MaxLength(70)]
        public string CompanyLogo { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? StartDay { get; set; }

        [MaxLength(20)]
        public string? EndDay { get; set; }

        [JsonConverter(typeof(CustomTimeSpanConverter))]
        public TimeSpan? StartHour { get; set; }

        [JsonConverter(typeof(CustomTimeSpanConverter))]
        public TimeSpan? EndHour { get; set; }

        [MaxLength(100)]
        public string? UserDesignation { get; set; }

        [MaxLength(100)]
        public string? AuthorisedContactPersonName { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        public DateTime? CRExpiryDate { get; set; } 

        [MaxLength(70)]
        public string? CoverImage1 { get; set; }

        [MaxLength(70)]
        public string? CoverImage2 { get; set; }

        public bool? IsTherapeuticService { get; set; }

        [MaxLength(70)]
        public string? TherapeuticCertificate { get; set; }

        [MaxLength(50)]
        public string? LicenseNumber { get; set; }
        [Required]
        public CompanyType CompanyType { get; set; }

        [Required]
        public CompanySize CompanySize { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public List<NatureOfBusiness> NatureOfBusiness { get; set; } = new();

        [Required, MaxLength(300)]
        public string BusinessDescription { get; set; } = string.Empty;

        [Required]
        public int CRNumber { get; set; }

        [Required, MaxLength(70)]
        public string CRDocument { get; set; } = string.Empty;

        public VerifiedStatus? Status { get; set; }

        [Required]
        public VerticalType Vertical { get; set; }

        public SubVertical? SubVertical { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        public bool? IsBasicProfile { get; set; }

        public bool IsActive { get; set; } = true;

        [Required, MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedUtc { get; set; }
    }
}
