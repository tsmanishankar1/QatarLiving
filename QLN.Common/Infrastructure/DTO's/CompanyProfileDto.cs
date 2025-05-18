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
        public int VerticalId { get; set; }
        public IFormFile? CompanyLogo { get; set; }
        [Required]
        public string BusinessName { get; set; }
        [Required]
        public string Country { get; set; }
        [Required]
        public string City { get; set; }
        public List<string>? BranchLocations { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? StartDay { get; set; }
        public string? EndDay { get; set; }
        public string? StartHour { get; set; }
        public string? EndHour { get; set; }
        [Required]
        public string NatureOfBusiness { get; set; }
        [Required]
        public string CompanySize { get; set; }
        [Required]
        public string CompanyType { get; set; }
        public string? UserDesignation { get; set; }
        [MaxLength(300)]
        public string? BusinessDescription { get; set; }
        [Required]
        public string CRNumber { get; set; }
        [Required]
        public IFormFile CRDocument { get; set; }
    }
    public class CompanyProfileEntity
    {
        public Guid Id { get; set; }
        public int VerticalId { get; set; }
        public string? CompanyLogo { get; set; }
        public string BusinessName { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public List<string>? BranchLocations { get; set; }
        public string PhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? StartDay { get; set; }
        public string? EndDay { get; set; }
        public string? StartHour { get; set; }
        public string? EndHour { get; set; }
        public string NatureOfBusiness { get; set; }
        public string CompanySize { get; set; }
        public string CompanyType { get; set; }
        public string? UserDesignation { get; set; }
        public string? BusinessDescription { get; set; }
        public string CRNumber { get; set; }
        public string? CRDocumentPath { get; set; }
    }
}
