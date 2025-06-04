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
    // Unique Queue Responses for New Qatar

    public class QlnNewsNewsQatar
    {
        const string QueuePrefix = DrupalContentConstants.QlnNewsNewsQatar;

        [JsonPropertyName($"{QueuePrefix}_top_story")]
        public BaseQueueResponse<ContentPost> TopStory { get; set; }

        [JsonPropertyName($"{QueuePrefix}_more_articles")]
        public BaseQueueResponse<ContentPost> MoreArticles { get; set; }

        [JsonPropertyName($"{QueuePrefix}_articles_1")]
        public BaseQueueResponse<ContentPost> Articles1 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_articles_2")]
        public BaseQueueResponse<ContentPost> Articles2 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_most_popular_articles")]
        public BaseQueueResponse<ContentPost> MostPopularArticles { get; set; }

        [JsonPropertyName($"{QueuePrefix}_watch_on_qatar_living")]
        public BaseQueueResponse<ContentVideo> WatchOnQatarLiving { get; set; }
    }

    public class QlnNewsNewsQatarPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnNewsNewsQatar)]
        public QlnNewsNewsQatar QlnNewsNewsQatar { get; set; }
    }

}
