using QLN.Common.DTO_s.Company;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s
{
    public class BaseCompanyDto
    {
        public Guid? Id { get; set; }
        [Required]
        public string CompanyName { get; set; } = string.Empty;
        [Required]
        public string Country { get; set; } = string.Empty;
        [Required]
        public string City { get; set; } = string.Empty;

        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        public string PhoneNumberCountryCode { get; set; } = string.Empty;
        public List<string>? BranchLocations { get; set; }
        [Required, Phone]
        public string WhatsAppNumber { get; set; }
        [Required]
        public string WhatsAppCountryCode { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }
        [Url]
        public string? WebsiteUrl { get; set; }
        [Url]
        public string? FacebookUrl { get; set; }
        [Url]
        public string? InstagramUrl { get; set; }
        [Required]
        public string CompanyLogo { get; set; } = string.Empty;
        public string? StartDay { get; set; } = string.Empty;
        public string? EndDay { get; set; } = string.Empty;
        [JsonConverter(typeof(CustomTimeSpanConverter))]
        public TimeSpan? StartHour { get; set; }
        [JsonConverter(typeof(CustomTimeSpanConverter))]
        public TimeSpan? EndHour { get; set; }

        [Required]
        public CompanyType CompanyType { get; set; }
        [Required]
        public CompanySize CompanySize { get; set; }
        [Required]
        public List<NatureOfBusiness> NatureOfBusiness { get; set; } 
        [Required, MaxLength(300)]
        public string BusinessDescription { get; set; } = string.Empty;
        [Required]
        public int CRNumber { get; set; }
        [Required]
        public string CRDocument { get; set; } = string.Empty;
        public bool? IsVerified { get; set; } = false;
        public VerifiedStatus? Status { get; set; }
        [Required]
        public VerticalType Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public string? UserId { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }
}
