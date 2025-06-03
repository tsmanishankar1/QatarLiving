using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class GetCommentsResponse
    {
        [JsonPropertyName("total_comments")]
        public int TotalComments { get; set; }

        [JsonPropertyName("per_page")]
        public string PerPage { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("comments")]
        public List<ContentComment> Comments { get; set; }
    }
}
