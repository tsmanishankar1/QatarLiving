using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Models
{
    public class CompanyProfileModel
    {
        public int VerticalId { get; set; }
        public int CategoryId { get; set; }
        public string UserId { get; set; }
        [Required(ErrorMessage = "Country is required")]
        public string Country { get; set; }
        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }
        public List<string> BranchLocations { get; set; }
        public string WhatsAppNumber { get; set; }
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Url(ErrorMessage = "Invalid website URL format")]
        public string WebsiteUrl { get; set; }

        [Url(ErrorMessage = "Invalid Facebook URL format")]
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string StartDay { get; set; }
        public string EndDay { get; set; }
        public string StartHour { get; set; }
        public string EndHour { get; set; }
        public string UserDesignation { get; set; }
        public string CrDocument { get; set; }
        public string Id { get; set; }
        [Required(ErrorMessage = "Business Name is required")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Company Logo is required")]
        public string CompanyLogo { get; set; }
        public int CompanyType { get; set; }
        public int CompanySize { get; set; }
        public string NatureOfBusiness { get; set; }
        public string BusinessDescription { get; set; }
        public int Status { get; set; }
        public int CrNumber { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedUtc { get; set; }

    }

    public enum CompanySize
    {
        [Display(Name = "0–10")]
        Size_0_10 = 1,

        [Display(Name = "11–50")]
        Size_11_50 = 2,

        [Display(Name = "51–200")]
        Size_51_200 = 3,

        [Display(Name = "201–500")]
        Size_201_500 = 4,

        [Display(Name = "500+")]
        Size_500_Plus = 5
    }

    public enum CompanyType
    {
        [Display(Name = "SME")]
        SME = 1,

        [Display(Name = "Enterprise")]
        Enterprise = 2,

        [Display(Name = "MNC")]
        MNC = 3,

        [Display(Name = "Government")]
        Government = 4
    }

    public enum NatureOfBusiness
    {
        [Display(Name = "Retail")]
        Retail = 1,

        [Display(Name = "Wholesale")]
        Wholesale = 2,

        [Display(Name = "Manufacturing")]
        Manufacturing = 3,

        [Display(Name = "IT Services")]
        ITServices = 4,

        [Display(Name = "Construction")]
        Construction = 5,

        [Display(Name = "Logistics")]
        Logistics = 6,
    }


}
