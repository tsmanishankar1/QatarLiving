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
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string Content { get; set; } = default!;
        public int? CommentsLikeCount { get; set; } = 0;
        public DateTime? CommentedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }
    }
}
