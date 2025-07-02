using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QLN.Web.Shared.Models
{
    public class CompanyProfileModel
    {
        public string Id { get; set; }
        public int Vertical { get; set; }
        public int SubVertical { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public List<string> BranchLocations { get; set; }
        public string WhatsAppNumber { get; set; }
   
        public string Email { get; set; }

        public string WebsiteUrl { get; set; }

        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string StartDay { get; set; }
        public string EndDay { get; set; }
        [JsonPropertyName("startHour")]
        public string StartHourRaw { get; set; }

        [JsonPropertyName("endHour")]
        public string EndHourRaw { get; set; }

        [JsonIgnore]
        public TimeSpan? StartHour
        {
            get => TimeSpan.TryParse(StartHourRaw, out var time) ? time : null;
            set => StartHourRaw = value?.ToString(@"hh\:mm");
        }

        [JsonIgnore]
        public TimeSpan? EndHour
        {
            get => TimeSpan.TryParse(EndHourRaw, out var time) ? time : null;
            set => EndHourRaw = value?.ToString(@"hh\:mm");
        }

        public string UserDesignation { get; set; }

        public string CrDocument { get; set; }

        public string BusinessName { get; set; }

        public string PhoneNumber { get; set; }

        public string CompanyLogo { get; set; }
        public int CompanyType { get; set; }
        public int CompanySize { get; set; }
        public List<int> NatureOfBusiness { get; set; } = new();
        public string BusinessDescription { get; set; }
        public int CrNumber { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string PhoneNumberCountryCode { get; set; }
        public string WhatsAppCountryCode { get; set; }

    }
    public class CompanyProfileModelDto
    {
        public int Vertical { get; set; }
        public int SubVertical { get; set; }
        [Required(ErrorMessage = "Country is required")]
        public string Country { get; set; }
        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }
        public List<string> BranchLocations { get; set; }

        [Phone(ErrorMessage = "Invalid Whatsapp number format")]
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
        public TimeSpan? StartHour { get; set; }
        public TimeSpan? EndHour { get; set; }
        public string UserDesignation { get; set; }

        [Required(ErrorMessage = "CR Document is required")]
        public string CrDocument { get; set; }

        public string Id { get; set; }
        [Required(ErrorMessage = "Business Name is required")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; }

        public string CompanyLogo { get; set; }
        public int CompanyType { get; set; }
        public int CompanySize { get; set; }
        public List<int> NatureOfBusiness { get; set; } = new();
        public string BusinessDescription { get; set; }
        public int CrNumber { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string PhoneNumberCountryCode { get; set; }
        public string WhatsAppCountryCode { get; set; }


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
    public enum DaysOfWeek
    {
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday
    }

    public class CountryCityModel
    {
        public string Country { get; set; }
        public List<string> Cities { get; set; }
        public string CountryCode { get; set; }
    }
}
