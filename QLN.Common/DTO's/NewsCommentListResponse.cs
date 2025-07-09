using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class NewsCommentListResponse
    {
        public int TotalComments { get; set; } = 0;    
        public int PerPage { get; set; }
        public int CurrentPage { get; set; }

        public List<NewsCommentListItem> Comments { get; set; } = new();
    }

    public class NewsCommentListItem
    {
        public Guid CommentId { get; set; }
        public int LikeCount { get; set; } = 0;
        public int DislikeCount { get; set; } = 0;
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Subject { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public List<UserSummary> LikedUsers { get; set; } = new();
        public List<UserSummary> DislikedUsers { get; set; } = new();
        public List<NewsCommentListItem>? Replies { get; set; } = new();
    }

    public class ReactionUser
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
    }

    public class UserSummary
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
    }

}
