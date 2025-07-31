namespace QLN.ContentBO.WebUI.Models
{
    public class FilterRequest
    {
        public string? Text { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
        public string? OrderBy { get; set; }
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public int? Status { get; set; }
        public string? SortField { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? SortDirection { get; set; }
        public string? SearchText { get; set; }
        public bool? IsPromoted { get; set; }
        public bool? IsFeatured { get; set; }
    }
    public class PrelovedResponse
    {
        public int TotalCount { get; set; }
        public List<PrelovedListing> ClassifiedsPreloved { get; set; } = new();
    }
    public class PrelovedListing
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Section { get; set; } 
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Whatsapp { get; set; }
        public decimal Amount { get; set; }
    }
}
