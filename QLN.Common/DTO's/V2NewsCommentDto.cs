using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class V2NewsCommentDto
    {
        public string Nid { get; set; }             // Article ID
        public string Uid { get; set; }             // User ID
        public string? UserName { get; set; }    // User Name
        public string Comment { get; set; } = "";
        public Guid CommentId { get; set; } = Guid.NewGuid();
        public DateTime CommentedAt { get; set; } = DateTime.UtcNow;
    }

    public class NewsCommentApiResponse
    {
        public string Status { get; set; } = "success";
        public string Message { get; set; } = "Comment saved successfully.";
    }
}
