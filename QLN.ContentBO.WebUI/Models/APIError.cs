namespace QLN.ContentBO.WebUI.Models
{
    public class APIError
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
    }
}
