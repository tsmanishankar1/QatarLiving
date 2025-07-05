using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class CommunityPostLikeDto
    {
        public Guid LikePostId { get; set; } = Guid.NewGuid();
        public Guid CommunityPostId { get; set; }        
        public string UserId { get; set; } = null!;
        public DateTime LikedDate { get; set; } = DateTime.UtcNow;
    }

    public class LikePostResponse
    {
        public string Status { get; set; } = "";
        public Guid CommunityPostId { get; set; }
        public string UserId { get; set; } = "";
    }
}
