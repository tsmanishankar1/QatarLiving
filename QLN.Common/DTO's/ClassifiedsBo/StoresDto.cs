using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Service.TimeSpanConverter;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace QLN.Common.DTO_s.ClassifiedsBo
{
    
    public class StoresSubscriptionDto
    {
        [Key]
        public int OrderId { get; set; }
        public string? SubscriptionType { get; set; } = null;
        public string? UserName { get; set; } = null;
        public string? Email { get; set; } = null;
        public string? Mobile { get; set; } = null;
        public string? Whatsapp { get; set; } = null;
        public string? WebUrl { get; set; } = null;
        public decimal Amount { get; set; } = 0;
        public string? Status { get; set; } = null;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WebLeads { get; set; } = 0;
        public int EmailLeads { get; set; } = 0;
        public int WhatsappLeads { get; set; } = 0;
        public int PhoneLeads { get; set; } = 0;

    }

    public class StoresDto
    {
        public string? UserName { get; set; } = null;
        public Guid? Id { get; set; }
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
        public bool? IsVerified { get; set; } = false;
        public CompanyStatus? Status { get; set; }
        [Required]
        public VerticalType Vertical { get; set; } = VerticalType.Classifieds;
        public SubVertical SubVertical { get; set; } = SubVertical.Stores;
        public string? UserId { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }
}
