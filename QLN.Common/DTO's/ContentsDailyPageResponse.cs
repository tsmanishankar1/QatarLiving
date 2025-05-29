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
    public class DailyEvent : BaseQueueResponse<ContentEvent>;
    public class DailyFeaturedEvents : BaseQueueResponse<ContentPost>; // think this should be ContentEvent, but keeping it as ContentPost for now to match the original structure
    //public class DailyFifaArabCup : BaseQueueResponse<ContentEvent>;
    public class DailyMoreArticles : BaseQueueResponse<ContentPost>;
    public class DailyTopStory : BaseQueueResponse<ContentPost>;
    public class DailyWatchOnQatarLiving : BaseQueueResponse<ContentPost>;

    public class ContentsDaily
    {
        [JsonPropertyName("daily_top_story")]
        public DailyTopStory DailyTopStory { get; set; }

        [JsonPropertyName("daily_event")]
        public DailyEvent DailyEvent { get; set; }

        [JsonPropertyName("daily_featured_events")]
        public DailyFeaturedEvents DailyFeaturedEvents { get; set; }

        [JsonPropertyName("daily_watch_on_qatar_living")]
        public DailyWatchOnQatarLiving DailyWatchOnQatarLiving { get; set; }

        //[JsonPropertyName("daily_fifa_arab_cup")]
        //public DailyFifaArabCup DailyFifaArabCup { get; set; }

        [JsonPropertyName("daily_more_articles")]
        public DailyMoreArticles DailyMoreArticles { get; set; }
    }

    public class ContentsDailyPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnContentsDaily)]
        public ContentsDaily ContentsDaily { get; set; }
    }
}
