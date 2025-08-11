namespace QLN.ContentBO.WebUI.Models
{
    public class PrelovedSubscriptionQuery
    {
        public string? SubscriptionType { get; set; }
        public string? FilterDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }
}
