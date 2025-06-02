using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class ChangeLikeStatusRequest
    {
        [JsonPropertyName("nid")]
        public int Nid { get; set; }

        [JsonPropertyName("uid")]
        public int Uid { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }
    }
}
