using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class NewsArticleDTO
    {
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public string WriterTag { get; set; }
       
        public string? CoverImageUrl { get; set; }
        
        public List<string> InlineImageUrls { get; set; } = [];

        public List<ArticleCategory> Categories { get; set; } = [];
        
        public DateTime PublishedDate { get; set; }
        
        public Guid CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid UpdatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
