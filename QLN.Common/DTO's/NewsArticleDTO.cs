
namespace QLN.Common.Infrastructure.DTO_s
{
    public class NewsArticleDTO
    {
        public Guid Id { get; set; }
        
        public string UserId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public string WriterTag { get; set; }

        public string? CoverImageUrl { get; set; }

        public string Slug { get; set; }

        public List<string> InlineImageUrls { get; set; } = [];

        public List<ArticleCategory> Categories { get; set; } = [];
          public class ArticleCategory
    {
        public int CategoryId { get; set; }
        
        public int SubcategoryId { get; set; }

        /// <summary>
        /// Defaults to UnPublished Slot
        /// </summary>
        public int SlotId { get; set; }
    }

        public bool IsActive { get; set; } = true;
        public DateTime PublishedDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string authorName { get; set; }
    }
}
