using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class V2NewsCommentDto
    {
        [JsonPropertyName("nid")]
        public string Nid { get; set; }

        [JsonPropertyName("uid")]
        public string Uid { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }

        [JsonPropertyName("commentId")]
        public Guid CommentId { get; set; } = Guid.NewGuid();

        [JsonPropertyName("commentedAt")]
        public DateTime CommentedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("parentCommentId")]
        public Guid? ParentCommentId { get; set; }
    }

    public class NewsCommentApiResponse
    {
        public string Status { get; set; } = "success";
        public string Message { get; set; } = "Comment saved successfully.";
    }
}
