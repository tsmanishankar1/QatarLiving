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
    // Unique Queue Responses for News World

    public class QlnNewsNewsWorld
    {
        const string _prefix = "qln_news_news_world";

        [JsonPropertyName($"{_prefix}_top_story")]
        public BaseQueueResponse<ContentPost> TopStory { get; set; }

        [JsonPropertyName($"{_prefix}_more_articles")]
        public BaseQueueResponse<ContentPost> MoreArticles { get; set; }

        [JsonPropertyName($"{_prefix}_articles_1")]
        public BaseQueueResponse<ContentPost> Articles1 { get; set; }

        [JsonPropertyName($"{_prefix}_articles_2")]
        public BaseQueueResponse<ContentPost> Articles2 { get; set; }

        [JsonPropertyName($"{_prefix}_most_popular_articles")]
        public BaseQueueResponse<ContentPost> MostPopularArticles { get; set; }
    }

    public class NewsWorldPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnNewsNewsWorld)]
        public QlnNewsNewsWorld QlnNewsNewsWorld { get; set; }
    }

}
