using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class CompanyProfileDto : BaseCompanyDto
    {
        public CompanyStatus? Status { get; set; }
        [Required]
        public VerticalType Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
        public string? UserId { get; set; }
        [Required]
        public string Country { get; set; } = string.Empty;
        [Required]
        public string City { get; set; } = string.Empty;
        public List<string>? BranchLocations { get; set; }
        public string? WhatsAppCountryCode { get; set; }
        public string? WhatsAppNumber { get; set; }
        [Required, EmailAddress]
        public string? Email { get; set; }
        [Url]
        public string? WebsiteUrl { get; set; }
        [Url]
        public string? FacebookUrl { get; set; } 
        [Url]
        public string? InstagramUrl { get; set; }
        public bool? IsTherapeuticService { get; set; }
        public string? TherapeuticCertificate { get; set; }
        [Required]
        public string StartDay { get; set; } = string.Empty;
        [Required]
        public string EndDay { get; set; } = string.Empty;
        [Required, JsonConverter(typeof(CustomTimeSpanConverter))]
        public TimeSpan StartHour { get; set; }
        [Required, JsonConverter(typeof(CustomTimeSpanConverter))]
        public TimeSpan EndHour { get; set; } 
        [Required]
        public string UserDesignation { get; set; } = string.Empty;
        [Required]
        public string CRDocument { get; set; } = string.Empty;
    }
    public class CompanyProfileCompletionStatusDto
    {
        public Guid? CompanyId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public int CompletionPercentage { get; set; }
        public List<string> PendingFields { get; set; } = new();
    }
    public class CompanyProfileVerificationStatusDto
    {
        public Guid? CompanyId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public VerticalType Vertical { get; set; }
        public bool? IsVerified { get; set; }
        public CompanyStatus? Status { get; set; } 
    }
    public class CompanyApproveDto
    {
        public Guid? CompanyId { get; set; }
        public bool? IsVerified { get; set; }
        public CompanyStatus? Status { get; set; }
        public string? RejectionReason { get; set; }
    }
    public class CompanyApprovalResponseDto
    {
        public Guid? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool? IsVerified { get; set; }
        public CompanyStatus? StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime? UpdatedUtc { get; set; }
    }
    public class ProfileStatus
    {
        public Guid? CompanyId { get; set; }
        public string? UserId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public VerticalType Vertical { get; set; } 
        public SubVertical SubVertical { get; set; }
        public bool IsActive { get; set; }
    }
}