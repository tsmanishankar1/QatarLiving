namespace QLN.ContentBO.WebUI.Models
{
    public class CommunityListDto
    {
        public int Number { get; set; }
        public string PostTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public string LiveFor { get; set; } = string.Empty;
    }
}
