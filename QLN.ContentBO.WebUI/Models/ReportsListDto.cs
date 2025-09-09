namespace QLN.ContentBO.WebUI.Models
{
    public class ReportsListDto
    {
        public int Number { get; set; }
        public string PostTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public string Reporter { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
    }
}
