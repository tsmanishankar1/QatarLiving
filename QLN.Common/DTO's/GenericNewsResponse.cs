using Microsoft.AspNetCore.Http.Features;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class GenericNewsPageResponse
    {

        [JsonPropertyName("top_story")]
        public BaseQueueResponse<ContentPost>? TopStory { get; set; }

        [JsonPropertyName("more_articles")]
        public BaseQueueResponse<ContentPost>? MoreArticles { get; set; }

        [JsonPropertyName("articles_1")]
        public BaseQueueResponse<ContentPost>? Articles1 { get; set; }

        [JsonPropertyName("articles_2")]
        public BaseQueueResponse<ContentPost>? Articles2 { get; set; }

        [JsonPropertyName("most_popular_articles")]
        public BaseQueueResponse<ContentPost>? MostPopularArticles { get; set; }

        [JsonPropertyName("watch_on_qatar_living")]
        public BaseQueueResponse<ContentVideo>? WatchOnQatarLiving { get; set; }
    }

    public class GeneralNewsResponse
    {
        [JsonPropertyName("news")]
        public GenericNewsPageResponse? News { get; set; }
    }
}


