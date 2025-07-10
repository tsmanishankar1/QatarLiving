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
    public class NewsContentV2
    {
        [JsonPropertyName("news")]
        public NewsSection News { get; set; }
    }

    public class NewsSection
    {
        [JsonPropertyName("top_story")]
        public BaseQueueResponseV2<ContentPost> TopStory { get; set; }

        [JsonPropertyName("more_articles")]
        public BaseQueueResponseV2<ContentPost> MoreArticles { get; set; }

        [JsonPropertyName("articles_1")]
        public BaseQueueResponseV2<ContentPost> Articles1 { get; set; }

        [JsonPropertyName("articles_2")]
        public BaseQueueResponseV2<ContentPost> Articles2 { get; set; }

        [JsonPropertyName("most_popular_articles")]
        public BaseQueueResponseV2<ContentPost> MostPopularArticles { get; set; }

        [JsonPropertyName("watch_on_qatar_living")]
        public BaseQueueResponseV2<ContentVideo> WatchOnQatarLiving { get; set; }
    }

    public class BaseQueueResponseV2<T>
    {
        [JsonPropertyName("queue_label")]
        public string QueueLabel { get; set; }

        [JsonPropertyName("items")]
        public List<T> Items { get; set; }
    }
}
