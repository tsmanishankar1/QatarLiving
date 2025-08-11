using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;

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

        [SearchableField(IsFilterable = true)]
        public string? Title { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string? Slug { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string? Category { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string? CategoryId { get; set; }

        [SearchableField]
        public string? Description { get; set; }

        [SimpleField(IsFilterable = true)]
        public string? ImageUrl { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public bool IsActive { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
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

    }
}
