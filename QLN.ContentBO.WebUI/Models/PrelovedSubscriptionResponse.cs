namespace QLN.ContentBO.WebUI.Models
{
    public class PrelovedSubscriptionResponse
    {
        public List<PrelovedSubscriptionItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
    }
}
