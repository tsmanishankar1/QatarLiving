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
    // Unique Queue Responses for News Community

    public class GenericNewsResults
    {
        [JsonPropertyName("top_story")]
        public TopStory TopStory { get; set; }

        [JsonPropertyName("more_articles")]
        public MoreArticles MoreArticles { get; set; }
    }

    public class GenericNewsPageResponse
    {
        [JsonPropertyName("results")]
        public GenericNewsResults Results { get; set; }
    }
}
