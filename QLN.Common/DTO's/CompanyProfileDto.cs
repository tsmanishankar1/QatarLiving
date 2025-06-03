using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class CompanyProfileDto
    {
        [Required]
        public VerticalType VerticalId { get; set; }
        public CompanyCategory? CategoryId { get; set; }
        public Guid UserId { get; set; }
        [Required]
        public string CompanyLogo { get; set; } = string.Empty;
        [Required]
        public string? BusinessName { get; set; } = string.Empty;
        [Required]
        public string Country { get; set; } = string.Empty;
        [Required]
        public string City { get; set; } = string.Empty;
        public List<string>? BranchLocations { get; set; }
        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        public string? WhatsAppNumber { get; set; }
        [Required, EmailAddress]
        public string? Email { get; set; }
        [Url]
        public string? WebsiteUrl { get; set; }
        [Url]
        public string? FacebookUrl { get; set; } 
        [Url]
        public string? InstagramUrl { get; set; }
        [Required]
        public string StartDay { get; set; } = string.Empty;
        [Required]
        public string EndDay { get; set; } = string.Empty;
        [Required, JsonConverter(typeof(CustomTimeSpanConverter))]
        public TimeSpan StartHour { get; set; }
        [Required, JsonConverter(typeof(CustomTimeSpanConverter))]
        public TimeSpan EndHour { get; set; } 
        [Required]
        public string NatureOfBusiness { get; set; } = string.Empty;
        [Required]
        public CompanySize CompanySize { get; set; }
        [Required]
        public CompanyType CompanyType { get; set; } 
        [Required]
        public string UserDesignation { get; set; } = string.Empty;
        [Required, MaxLength(300)]
        public string BusinessDescription { get; set; } = string.Empty;
        [Required]
        public int CRNumber { get; set; }
        [Required]
        public string CRDocument { get; set; } = string.Empty;
        public bool? IsVerified { get; set; } = false;
        public CompanyStatus? Status { get; set; }
    }
    public class CompanyProfileEntity : CompanyProfileDto
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }
    public class CompanyProfileCompletionStatusDto
    {
        public Guid CompanyId { get; set; }
        public string? BusinessName { get; set; }
        public int CompletionPercentage { get; set; }
        public List<string> PendingFields { get; set; } = new();
    }
    public class CompanyProfileVerificationStatusDto
    {
        public Guid CompanyId { get; set; }
        public string BusinessName { get; set; } = null!;
        public VerticalType VerticalId { get; set; }
        public bool? IsVerified { get; set; }
        public string Status { get; set; } = "Pending"; 
    }
    public class CompanyApproveDto
    {
        public Guid CompanyId { get; set; }
        public bool? IsVerified { get; set; }
        public CompanyStatus? Status { get; set; }
    }
    public class CompanyApprovalResponseDto
    {
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = default!;
        public bool? IsVerified { get; set; }
        public CompanyStatus? StatusId { get; set; }
        public string StatusName { get; set; } = default!;
        public DateTime? UpdatedUtc { get; set; }
    }
}