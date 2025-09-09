using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Company
{
    public class CompanySubscriptionDto
    {
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string WhatsApp { get; set; }
        public string WebUrl { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Slug { get; set; }
        [JsonIgnore]
        public string? SubscriptionType { get; set; }
    }
    public class CompanySubscriptionFilter
    {
        public string? SubscriptionType { get; set; }
        public DateTime? StartDate { get; set; } 
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; } 
        public string? SortBy { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }
    public class CompanySubscriptionListResponseDto
    {
        public List<CompanySubscriptionDto> Records { get; set; }
        public int TotalRecords { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int TotalPages { get; set; }
    }
    public class CompanyWithSubscriptionDto
    {
        public Guid Id { get; set; }
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
        public string Email { get; set; }
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
        public string? LicenseNumber { get; set; }
        public VerifiedStatus? CompanyVerificationStatus { get; set; }
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
        public string? AuthorisedContactPersonName { get; set; }
        public DateTime? CRExpiryDate { get; set; }
        [Required]
        public string CRDocument { get; set; } = string.Empty;
        public string UploadFeed { get; set; } = string.Empty;
        public string XMLFeed { get; set; } = string.Empty;
        public VerifiedStatus? Status { get; set; }
        [Required]
        public VerticalType Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public string? StoresURL { get; set; }
        public string? ImportType { get; set; }
        public bool? IsActive { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public bool? IsBasicProfile { get; set; }
        public string? Slug { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public string? ProductName { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }
}
