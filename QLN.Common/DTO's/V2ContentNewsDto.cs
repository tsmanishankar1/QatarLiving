using System;
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{

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

        public List<V2ArticleCategory> Categories { get; set; } = [];

        public DateTime PublishedDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }
        public string authorName { get; set; }

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
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
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
