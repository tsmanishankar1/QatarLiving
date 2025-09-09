namespace QLN.ContentBO.WebUI.Models
{
    public class AdImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string AdImageFileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}