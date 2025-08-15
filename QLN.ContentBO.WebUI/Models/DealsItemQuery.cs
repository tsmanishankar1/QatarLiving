namespace QLN.ContentBO.WebUI.Models
{
    public class DealsItemQuery
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Search { get; set; }
        public string SortBy { get; set; }
        public string Status { get; set; }
        public bool? IsPromoted { get; set; }
        public bool? IsFeatured { get; set; }
    }
}
