namespace QLN.ContentBO.WebUI.Models
{
    public class VerificationProfileStatus 
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PhoneNumberCountryCode { get; set; } = string.Empty;
        public List<string>? BranchLocations { get; set; }
        public string WhatsAppNumber { get; set; }
        public string WhatsAppCountryCode { get; set; }
        public string Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string CompanyLogo { get; set; } = string.Empty;
        public string? StartDay { get; set; } = string.Empty;
        public string? EndDay { get; set; } = string.Empty;
        public TimeSpan? StartHour { get; set; }
        public TimeSpan? EndHour { get; set; }
        public string? UserDesignation { get; set; }
        public string? CoverImage1 { get; set; }
        public string? CoverImage2 { get; set; }
        public bool? IsTherapeuticService { get; set; }
        public string? TherapeuticCertificate { get; set; }
        public string? LicenseNumber { get; set; }
        public string BusinessDescription { get; set; } = string.Empty;
        public int CRNumber { get; set; }
        public string CRDocument { get; set; } = string.Empty;
        public VerifiedStatus? Status { get; set; }
        public VerticalType Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
    }
    public enum VerticalType
    {
        Jobs = 1,
        Properties = 2,
        Classifieds = 3,
        Services = 4
    }
    public enum SubVerticalType
    {
        Items = 1,
        Deals = 2,
        Stores = 3,
        Preloved = 4,
        Collectibles = 5,
        Services = 6,
        News = 7,
        Daily = 8,
        Events = 9,
        Community = 10
    }
    public class PaginatedCompanyResponse
    {
        public List<VerificationProfileStatus> items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PerSize { get; set; }
    }
    


}