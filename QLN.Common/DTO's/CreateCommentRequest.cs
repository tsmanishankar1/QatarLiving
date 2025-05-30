using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class CreateCommentRequest
    {
        [JsonPropertyName("nid")]
        public int ArticleId { get; set; }

        [JsonPropertyName("uid")]
        public int Uid { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }
}
