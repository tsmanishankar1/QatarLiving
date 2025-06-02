using Microsoft.AspNetCore.Http.Features;
using QLN.Common.Infrastructure.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{

    public class CommunitiesResponse
    {
        [JsonPropertyName("items")]
        public List<CommunityPost> Items { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }
    }
}
