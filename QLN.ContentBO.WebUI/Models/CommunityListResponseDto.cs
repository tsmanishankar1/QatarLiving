namespace QLN.ContentBO.WebUI.Models
{
    public class CommunityPostDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }
        public bool IsActive { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedDate { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class CommunityListResponseDto
    {
        public int Total { get; set; }
        public List<CommunityPostDto> Items { get; set; } = new();
    }
}
