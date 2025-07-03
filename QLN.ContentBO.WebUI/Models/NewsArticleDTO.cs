using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class NewsArticleDTO
    {
        public Guid Id { get; set; }
        
        public string UserId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public string WriterTag { get; set; }

        public string? CoverImageUrl { get; set; }

        public string Slug { get; set; }

        public List<string> InlineImageUrls { get; set; } = [];

        [Required]
        public List<ArticleCategory> Categories { get; set; } = [];

        public bool IsActive { get; set; } = true;
        public DateTime PublishedDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string authorName { get; set; }
    }
}
