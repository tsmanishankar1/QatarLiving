using Azure.Search.Documents.Indexes;
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class ContentNewsIndex
    {
        // Primary key
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        [SimpleField(IsFilterable = true)]
        public string UserId { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true, IsSortable = true)]
        public string Title { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string Content { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string WriterTag { get; set; }

        public string? CoverImageUrl { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string Slug { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsActive { get; set; }

        public IList<ArticleCategory>? Categories { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? PublishedDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public string CreatedBy { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime CreatedAt { get; set; }

        [SimpleField(IsFilterable = true)]
        public string UpdatedBy { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime UpdatedAt { get; set; }

        [SimpleField(IsFilterable = true)]
        public string authorName { get; set; }
    }

    public class ArticleCategory
    {
        [SimpleField(IsFilterable = true)]
        public int CategoryId { get; set; }

        [SimpleField(IsFilterable = true)]
        public int SubcategoryId { get; set; }

        [SimpleField(IsFilterable = true)]
        public int SlotId { get; set; }
    }
}