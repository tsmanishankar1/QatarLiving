using Azure.Search.Documents.Indexes;
using System.Text.Json.Serialization;

namespace QLN.Common.DTO_s
{
    public class ContentCommunityIndex
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string? UserName { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string? UserId { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string? Title { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string? Slug { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string? Category { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string? CategoryId { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? Description { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? ImageUrl { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public bool IsActive { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string? UpdatedBy { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? UpdatedDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime? DateCreated { get; set; }

        [SimpleField(IsFilterable = true)]
        public int LikeCount { get; set; }

        [SimpleField(IsFilterable = true)]
        public int CommentCount { get; set; }

        [SimpleField(IsFilterable = true)]
        public IList<string>? LikedUserIds { get; set; }

        [SimpleField(IsFilterable = true)]
        public IList<string>? CommentedUserIds { get; set; }

        public virtual IList<ContentCommunityIndex>? MoreArticles { get; set; }  // Kishore: Should likely be dynamically generated from an Index search now
                                                                                 // so wouldnt be in the index, but in the results as something we can update
                                                                                 // with this value - making this virtual asusming this maybe works like a
                                                                                 // DBContext
    }
}