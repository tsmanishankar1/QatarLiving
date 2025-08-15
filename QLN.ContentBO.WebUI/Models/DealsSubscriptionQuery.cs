namespace QLN.ContentBO.WebUI.Models
{
    public class DealsSubscriptionQuery
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SubscriptionType { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Search { get; set; }
        public string SortBy { get; set; }
    }
}
