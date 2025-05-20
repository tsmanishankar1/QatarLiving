using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class CompanyProfileDto
    {
        [Required]
        public int VerticalId { get; set; }
        public Guid UserId { get; set; }
        //public IFormFile? CompanyLogo { get; set; }
        public string? BusinessName { get; set; }
        [Required]
        public string Country { get; set; } = string.Empty;
        [Required]
        public string City { get; set; } = string.Empty;
        public string? Branches { get; set; }
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        public string? WhatsAppNumber { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        [Url]
        public string? WebsiteUrl { get; set; }
        [Url]
        public string? FacebookUrl { get; set; } 
        [Url]
        public string? InstagramUrl { get; set; }
        public string? StartDay { get; set; }
        public string? EndDay { get; set; }
        public string? StartHour { get; set; }
        public string? EndHour { get; set; }
        [Required]
        public string NatureOfBusiness { get; set; } = string.Empty;
        [Required]
        public string CompanySize { get; set; } = string.Empty;
        [Required]
        public string CompanyType { get; set; } = string.Empty;
        [Required]
        public string UserDesignation { get; set; } = string.Empty;
        [Required, MaxLength(300)]
        public string BusinessDescription { get; set; } = string.Empty;
        [Required]
        public string CRNumber { get; set; } = string.Empty;
        //[Required]
        //public IFormFile CRDocument { get; set; } = default!;
    }
    public class CompanyProfileEntity
    {
        public Guid Id { get; set; }
        public int VerticalId { get; set; }
        public Guid UserId { get; set; }
        public string? CompanyLogo { get; set; }
        public string? BusinessName { get; set; }
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? Branches { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? WhatsAppNumber { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? FacebookUrl { get; set; } 
        public string? InstagramUrl { get; set; } 
        public string? StartDay { get; set; }
        public string? EndDay { get; set; }
        public string? StartHour { get; set; }
        public string? EndHour { get; set; }
        public string NatureOfBusiness { get; set; } = string.Empty;
        public string CompanySize { get; set; } = string.Empty;
        public string CompanyType { get; set; } = string.Empty;
        public string UserDesignation { get; set; } = string.Empty;
        public string BusinessDescription { get; set; } = string.Empty;
        public string CRNumber { get; set; } = string.Empty;
        public string CRDocument { get; set; } = string.Empty;
    }
}