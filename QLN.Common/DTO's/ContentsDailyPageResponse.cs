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

        [JsonPropertyName($"{QueuePrefix}_top_stories")]
        public BaseQueueResponse<ContentPost> DailyTopStories { get; set; }

        [JsonPropertyName($"{QueuePrefix}_event")]
        public BaseQueueResponse<ContentEvent> DailyEvent { get; set; }

        [JsonPropertyName($"{QueuePrefix}_featured_events")]
        public BaseQueueResponse<ContentEvent> DailyFeaturedEvents { get; set; }

        [JsonPropertyName($"{QueuePrefix}_watch_on_qatar_living")]
        public BaseQueueResponse<ContentVideo> DailyWatchOnQatarLiving { get; set; }

        [JsonPropertyName($"{QueuePrefix}_more_articles")]
        public BaseQueueResponse<ContentEvent> DailyMoreArticles { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_1")]
        public BaseQueueResponse<ContentEvent> DailyTopics1 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_2")]
        public BaseQueueResponse<ContentEvent> DailyTopics2 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_3")]
        public BaseQueueResponse<ContentEvent> DailyTopics3 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_4")]
        public BaseQueueResponse<ContentEvent> DailyTopics4 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_topics_5")]
        public BaseQueueResponse<ContentEvent> DailyTopics5 { get; set; }
    }

    public class ContentsDailyPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnContentsDaily)]
        public ContentsDaily ContentsDaily { get; set; }
    }
    public class QlnEventsResponse
    {
        [JsonPropertyName("qln_events")]
        public QlnEvents QlnEvents { get; set; }
    }

}