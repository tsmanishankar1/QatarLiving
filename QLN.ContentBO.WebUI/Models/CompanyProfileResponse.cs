namespace QLN.ContentBO.WebUI.Models
{
    public class CompanyProfileResponse
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<CompanyProfileItem> Items { get; set; } = new();
    }

    public class CompanyProfileItem
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PhoneNumberCountryCode { get; set; } = string.Empty;
        public List<string> BranchLocations { get; set; } = new();
        public string WhatsAppNumber { get; set; } = string.Empty;
        public string WhatsAppCountryCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string FacebookUrl { get; set; } = string.Empty;
        public string InstagramUrl { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
        public string StartDay { get; set; } = string.Empty;
        public string EndDay { get; set; } = string.Empty;
        public string StartHour { get; set; } = string.Empty;
        public string EndHour { get; set; } = string.Empty;
        public string UserDesignation { get; set; } = string.Empty;
        public string AuthorisedContactPersonName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CrExpiryDate { get; set; }
        public string CoverImage1 { get; set; } = string.Empty;
        public string CoverImage2 { get; set; } = string.Empty;
        public bool IsTherapeuticService { get; set; }
        public string TherapeuticCertificate { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int CompanyType { get; set; }
        public int CompanySize { get; set; }
        public List<int> NatureOfBusiness { get; set; } = new();
        public string BusinessDescription { get; set; } = string.Empty;
        public long CrNumber { get; set; }
        public string CrDocument { get; set; } = string.Empty;
        public int Status { get; set; }
        public int Vertical { get; set; }
        public int SubVertical { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsBasicProfile { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedUtc { get; set; }
    }
}
