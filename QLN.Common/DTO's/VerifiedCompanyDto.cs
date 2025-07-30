using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class VerifiedCompanyDto : BaseCompanyDto
    {
        [Required]
        public string AuthorisedContactPersonName { get; set; }
        public string? UserDesignation { get; set; }
        public string? UserName { get; set; }

        [Required]
        public DateOnly CRExpiryDate { get; set; }
    }
    public class CompanyVerificationStatusDto
    {
        public Guid? CompanyId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public VerticalType Vertical { get; set; }
        public bool? IsVerified { get; set; }
        public CompanyStatus? Status { get; set; }
    }
    public class CompanyVerificationApproveDto
    {
        public Guid? CompanyId { get; set; }
        public bool? IsVerified { get; set; }
        public CompanyStatus? Status { get; set; }
        public string? RejectionReason { get; set; }
    }
    public class CompanyVerifyApprovalResponseDto
    {
        public Guid? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool? IsVerified { get; set; }
        public CompanyStatus? StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime? UpdatedUtc { get; set; }
    }
    public class VerificationProfileStatus
    {
        public Guid? CompanyId { get; set; }
        public string? UserId { get; set; }
        public string? CRFile { get; set; }
        public int? CRLicense { get; set; }
        public DateOnly Enddate { get; set; }
        public string? Username { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public VerticalType Vertical { get; set; }
        public SubVertical SubVertical { get; set; }
        public bool IsActive { get; set; }
    }
}
