using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class CompanyProfileDto : BaseCompanyDto
    {
        [Required]
        public string UserDesignation { get; set; }
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