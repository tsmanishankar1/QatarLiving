using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class CommunityCommentDto
    {
        public Guid CommentId { get; set; } = Guid.NewGuid();
        public Guid CommunityPostId { get; set; }
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int CommentsLikeCount { get; set; } = 0;
        public int CommentsDislikeCount { get; set; } = 0;
        public DateTime CommentedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }
    }
    public class CommunityCommentListResponse
    {
        public int TotalComments { get; set; }
        public int PerPage { get; set; }
        public int CurrentPage { get; set; }
        public List<CommunityCommentDto> Comments { get; set; } = new();
    }


}
