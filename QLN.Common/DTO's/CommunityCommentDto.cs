
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class CommunityCommentDto
    {
        public Guid CommentId { get; set; }
        public Guid CommunityPostId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CommentedAt { get; set; }
        public Guid? ParentCommentId { get; set; } 
        public bool IsActive { get; set; } = true;
        public DateTime UpdatedAt { get; set; }
        public int CommentsLikeCount { get; set; }
        public List<string>? LikedUserIds { get; set; } 
    }
    public class CommunityCommentItem
    {
        public Guid CommentId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CommentedAt { get; set; }
        public int LikeCount { get; set; }
        public bool? IsLiked { get; set; }
        public List<string>? LikedUserIds { get; set; }
        public string CommentedUserId { get; set; } = string.Empty;
        public List<CommunityCommentItem> Replies { get; set; } = new();
    }
    public class CommunityCommentListResponse
    {
        public int TotalComments { get; set; }
        public int PerPage { get; set; }
        public int CurrentPage { get; set; }
        public List<string>? CommentedUserIds { get; set; } 
        public List<CommunityCommentItem> Comments { get; set; } = new();
    }
    public class EditCommunityCommentDto
    {
        public Guid CommunityPostId { get; set; }
        public Guid CommentId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
    public class LikeCommentsDto
    {
        public Guid CommentId { get; set; }
        public Guid CommunityPostId { get; set; }
    }

}
