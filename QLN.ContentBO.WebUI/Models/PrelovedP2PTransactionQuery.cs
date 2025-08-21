namespace QLN.ContentBO.WebUI.Models
{
    public class PrelovedP2PTransactionQuery
    {
        public string? CreatedDate { get; set; }
        public string? PublishedDate { get; set; }
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }
}
