namespace QLN.ContentBO.WebUI.Models
{
    public class ItemsRequest
    {
        public string? Text { get; set; }
        public bool? IsFeatured { get; set; }
        public bool? IsPromoted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? PublishedDate { get; set; }
        public int? Status { get; set; }
        public int? AdType { get; set; }
        public string? OrderBy { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }
}
