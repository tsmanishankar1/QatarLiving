namespace QLN.ContentBO.WebUI.Models
{

    public class PrelovedP2PSubscriptionResponse
    {
        public List<PrelovedP2PSubscriptionItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
    }

    public class PrelovedP2PSubscriptionQuery
    {
        public string? Status { get; set; }
        public string? CreatedDate { get; set; }
        public string? PublishedDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }

    public class PrelovedP2PSubscriptionItem
    {
        public long AdId { get; set; }
        public int OrderId { get; set; }
        public string? AdType { get; set; }
        public int UserId { get; set; }
        public string? AdTitle { get; set; }
        public string? UserName { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Brand { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? Status { get; set; }
        public string? ImageUrl { get; set; }
        public int Views { get; set; }
        public int Impressions { get; set; }
        public int WhatsAppLeads { get; set; }
        public int PhoneLeads { get; set; }
        public int Share { get; set; }
        public int Feature { get; set; }
        public DateTime CreatedDateForApi { get; set; }
        public DateTime PublishedDateForApi { get; set; }
        public DateTime ExpiryDateForApi { get; set; }
    }
}
