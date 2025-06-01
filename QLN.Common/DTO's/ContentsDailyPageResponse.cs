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
    // Unique Queue Responses for Daily Content
    public class ContentsDaily
    {
        const string QueuePrefix = DrupalContentConstants.QlnContentsDaily;

        [JsonPropertyName($"{QueuePrefix}_top_story")]
        public BaseQueueResponse<ContentPost> DailyTopStory { get; set; }

        [JsonPropertyName($"{QueuePrefix}_event")]
        public BaseQueueResponse<ContentEvent> DailyEvent { get; set; }

        [JsonPropertyName($"{QueuePrefix}_featured_events")]
        public BaseQueueResponse<ContentPost> DailyFeaturedEvents { get; set; }

        [JsonPropertyName($"{QueuePrefix}_watch_on_qatar_living")]
        public BaseQueueResponse<ContentPost> DailyWatchOnQatarLiving { get; set; }

        [JsonPropertyName($"{QueuePrefix}_more_articles")]
        public BaseQueueResponse<ContentPost> DailyMoreArticles { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_1")]
        public BaseQueueResponse<ContentPost> DailyTopics1 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_2")]
        public BaseQueueResponse<ContentPost> DailyTopics2 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_3")]
        public BaseQueueResponse<ContentPost> DailyTopics3 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_4")]
        public BaseQueueResponse<ContentPost> DailyTopics4 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_5")]
        public BaseQueueResponse<ContentPost> DailyTopics5 { get; set; }
    }

    public class ContentsDailyPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnContentsDaily)]
        public ContentsDaily ContentsDaily { get; set; }
    }
}