namespace QLN.ContentBO.WebUI.Models
{
    public class CompanySubscriptionDto
    {
        public Guid companyId { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string WhatsApp { get; set; }
        public string WebUrl { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? SubscriptionType { get; set; }

    }
    public class CompanyStoresResponse
    {
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<CompanySubscriptionDto> Records { get; set; } = new();
    }

   
}