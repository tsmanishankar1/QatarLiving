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
}
