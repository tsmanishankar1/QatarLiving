using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class DealsStoresCompanyDto : BaseCompanyDto
    {
        [Required]
        public string UserDesignation { get; set; }
        public string? CoverImage1 { get; set; }
        public string? CoverImage2 { get; set; }

    }
    public class CompanyDsVerificationStatusDto
    {
        public Guid? CompanyId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public VerticalType Vertical { get; set; }
        public bool? IsVerified { get; set; }
        public CompanyStatus? Status { get; set; }
    }
    public class CompanyDsApproveDto
    {
        public Guid? CompanyId { get; set; }
        public bool? IsVerified { get; set; }
        public CompanyStatus? Status { get; set; }
        public string? RejectionReason { get; set; }
    }
    public class CompanyDsApprovalResponseDto
    {
        public Guid? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool? IsVerified { get; set; }
        public CompanyStatus? StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime? UpdatedUtc { get; set; }
    }
    public class DsProfileStatus
    {
        public Guid? CompanyId { get; set; }
        public string? UserId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public VerticalType Vertical { get; set; }
        public SubVertical SubVertical { get; set; }
        public bool IsActive { get; set; }
    }
}
