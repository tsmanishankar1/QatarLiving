namespace QLN.ContentBO.WebUI.Models
{
    public class Writertag
    {
        public Guid TagId { get; set; }
        public string Tagname { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
