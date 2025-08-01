using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s.Company
{
    public class CompanyProfile
    {
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
        public string? UserDesignation { get; set; }
        public string? CoverImage1 { get; set; }
        public string? CoverImage2 { get; set; }
        public bool? IsTherapeuticService { get; set; }
        public string? TherapeuticCertificate { get; set; }
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
        public CompanyStatus? Status { get; set; }
        [Required]
        public VerticalType Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public string? AuthorisedContactPersonName { get; set; }
        public DateOnly? CRExpiryDate { get; set; }
    }
    public class CompanyRequest : CompanyProfile
    {
        public string UserName { get; set; }
        public string CreatedBy { get; set; }
    }
}
