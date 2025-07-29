using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s
{
    public class ServiceCompanyDto : BaseCompanyDto
    {
        [Required]
        public bool IsTherapeuticService { get; set; }
        public string? TherapeuticCertificate { get; set; }
    }
    public class CompanyServiceVerificationStatusDto
    {
        public Guid? CompanyId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public VerticalType Vertical { get; set; }
        public bool? IsVerified { get; set; }
        public CompanyStatus? Status { get; set; }
    }
    public class CompanyServiceApproveDto
    {
        public Guid? CompanyId { get; set; }
        public bool? IsVerified { get; set; }
        public CompanyStatus? Status { get; set; }
        public string? RejectionReason { get; set; }
    }
    public class CompanyServiceApprovalResponseDto
    {
        public Guid? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool? IsVerified { get; set; }
        public CompanyStatus? StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime? UpdatedUtc { get; set; }
    }
    public class ServiceProfileStatus
    {
        public Guid? CompanyId { get; set; }
        public string? UserId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public VerticalType Vertical { get; set; }
        public SubVertical SubVertical { get; set; }
        public bool IsActive { get; set; }
    }
}
