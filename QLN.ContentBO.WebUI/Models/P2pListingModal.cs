namespace QLN.ContentBO.WebUI.Models
{
    public class PagedResult<T>
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<T> Items { get; set; } = new List<T>();
    }
    public class P2pListingModal
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AdTitle { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Section { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPromoted { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateExpiry { get; set; }
        public List<ImageUpload>? ImageUpload { get; set; }
        public string OrderId { get; set; } = string.Empty;
    }
    public class DealsListingModal
    {
        public string Id {  get; set; }
        public string AdId { get; set; } = string.Empty;
        public string DealTitle { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ContactNumber {  get; set; } = string.Empty;
        public string WhatsappNumber { get; set; } = string.Empty;
        public string? WebUrL { get; set; }
        public string? SubscriptionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public DateTime? DateCreated { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public List<ImageUpload>? ImageUpload { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string WebClick { get; set; }
        public string Views { get; set; }
        public string Impression {  get; set; }
        public string PhoneLead { get; set; }
    }
    public class ImageUpload
    {
        public string Url { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
