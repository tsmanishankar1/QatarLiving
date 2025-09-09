namespace QLN.ContentBO.WebUI.Models
{
    public class CompanyRequestPayload
    {
        public bool IsBasicProfile { get; set; }
        public int Status { get; set; }
        public int Vertical { get; set; }
        public int SubVertical { get; set; }
        public string Search { get; set; } = string.Empty;
        public string SortBy { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
    public class CompanySubscriptionFilter
    {
        public string? SubscriptionType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }

}
