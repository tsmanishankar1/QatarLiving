using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class BaseCompanyDto
    {
        public Guid? Id { get; set; }
        [Required]
        public string BusinessName { get; set; } = string.Empty;
        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        public string PhoneNumberCountryCode { get; set; } = string.Empty;
        [Required]
        public string CompanyLogo { get; set; } = string.Empty;
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
        public bool? IsVerified { get; set; } = false;
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }
}
