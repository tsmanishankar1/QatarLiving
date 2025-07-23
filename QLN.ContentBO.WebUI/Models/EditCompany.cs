namespace QLN.ContentBO.WebUI.Models
{
    public class EditCompany
    {
        public string? Title { get; set; }         
        public string? Location { get; set; }        
        public string? Description { get; set; }              // Rich text description
        public string? PhoneNumber { get; set; }
        public string? PhoneCode { get; set; }                // Country code for Phone
        public string? WhatsappNumber { get; set; }
        public string? WhatsappCode { get; set; }             // Country code for WhatsApp
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public DateTime? StartDay { get; set; }
        public DateTime? EndDay { get; set; }
        public TimeSpan? StartHour { get; set; }
        public TimeSpan? EndHour { get; set; }

        // Company Profile Options (selected items from dropdowns)
        public string? BusinessNature { get; set; }
        public string? CompanySize { get; set; }
        public string? CompanyType { get; set; }

        public string? UserDesignation { get; set; }

        // CR number (XmlLink used for naming legacy reasons?)
        public string? XmlLink { get; set; }

        // Certificate File
        public string? Certificate { get; set; }              // Base64-encoded file
        public string? CertificateFileName { get; set; }

        // Cover & Logo
        public string? CoverImageBase64 { get; set; }
        public string? CompanyLogoBase64 { get; set; }

        // Optional - If you want to track country/city selected separately from FieldOptions
        public string? SelectedCountry { get; set; }
        public string? SelectedCity { get; set; }
    }
}
