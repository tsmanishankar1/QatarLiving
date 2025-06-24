using System;
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{


    // Main DTO
    public class V2ContentNewsDto
    {
        public Guid Id { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Image_url { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string date_created { get; set; }
        public string publishedDate { get; set; }
        public string authorName { get; set; }
        public string title { get; set; }

        [MaxLength(2000)]
        public string description { get; set; }
        public List<string> WriterTag { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public StatusType Status { get; set; }
    }


    // DTO for Category, which includes SubCategory
    public class NewsCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<NewsCategoryDto>? SubCategory { get; set; }
    }

    public enum StatusType
    {
        Unpublished,
        Published

    }


    // new dto 
    public class V2ArticleCategory
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int SubcategoryId { get; set; }
        public int SlotId { get; set; }
    }
    public class V2NewsArticleDTO
    {
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public List<string> WriterTag { get; set; }

        public string? CoverImageUrl { get; set; }

        public List<string> InlineImageUrls { get; set; } = [];

        public List<V2ArticleCategory> Categories { get; set; } = [];

        public DateTime PublishedDate { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid UpdatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class V2NewsCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public List<V2NewsSubCategory> SubCategories { get; set; }
    }
    public class V2NewsSubCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
    }
    public class V2Slot
    {
        public Slot Id { get; set; }

        public string Name { get; set; }
    }
    public enum Slot
    {
        Slot1 = 1,
        Slot2 = 2,
        Slot3 = 3,
        Slot4 = 4,
        Slot5 = 5,
        Slot6 = 6,
        Slot7 = 7,
        Slot8 = 8,
        Slot9 = 9,
        Slot10 = 10,
        Slot11 = 11,
        Slot12 = 12,
        Slot13 = 13,
        Published = 14,
        UnPublished = 15
    }

}
