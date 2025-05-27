using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class BaseQueueResponse<T>
    {
        [JsonPropertyName("queue_label")]
        public string QueueLabel { get; set; } = string.Empty;
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = new List<T>();
    }
    // Unique Queue Responses for Daily Content
    public class DailyEvent : BaseQueueResponse<ContentEvent>;
    public class DailyFeaturedEvents : BaseQueueResponse<ContentPost>;
    public class DailyFifaArabCup : BaseQueueResponse<ContentEvent>;
    public class DailyMoreArticles : BaseQueueResponse<ContentPost>;
    public class DailyTopStory : BaseQueueResponse<ContentPost>;
    public class DailyWatchOnQatarLiving : BaseQueueResponse<ContentPost>;

    public class ContentEvent : ContentPost
    {
        [JsonPropertyName("entity_organizer")]
        public string EntityOrganizer { get; set; }

        [JsonPropertyName("event_venue")]
        public string EventVenue { get; set; }

        [JsonPropertyName("event_start")]
        public string EventStart { get; set; }

        [JsonPropertyName("event_end")]
        public string EventEnd { get; set; }

        [JsonPropertyName("event_lat")]
        public string EventLat { get; set; }

        [JsonPropertyName("event_long")]
        public string EventLong { get; set; }

        [JsonPropertyName("event_location")]
        public string EventLocation { get; set; }
    }


    public class ContentPost
    {
        [JsonPropertyName("page_name")]
        public string PageName { get; set; }

        [JsonPropertyName("queue_name")]
        public string QueueName { get; set; }

        [JsonPropertyName("queue_label")]
        public string QueueLabel { get; set; }

        [JsonPropertyName("node_type")]
        public string NodeType { get; set; }

        [JsonPropertyName("nid")]
        public string Nid { get; set; }

        [JsonPropertyName("date_created")]
        public string DateCreated { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

    }

    public class QlnContentsDaily
    {
        [JsonPropertyName("daily_top_story")]
        public DailyTopStory DailyTopStory { get; set; }

        [JsonPropertyName("daily_event")]
        public DailyEvent DailyEvent { get; set; }

        [JsonPropertyName("daily_featured_events")]
        public DailyFeaturedEvents DailyFeaturedEvents { get; set; }

        [JsonPropertyName("daily_watch_on_qatar_living")]
        public DailyWatchOnQatarLiving DailyWatchOnQatarLiving { get; set; }

        [JsonPropertyName("daily_fifa_arab_cup")]
        public DailyFifaArabCup DailyFifaArabCup { get; set; }

        [JsonPropertyName("daily_more_articles")]
        public DailyMoreArticles DailyMoreArticles { get; set; }
    }

    public class ContentLandingPageResponse
    {
        [JsonPropertyName("qln_contents_daily")]
        public QlnContentsDaily QlnContentsDaily { get; set; }
    }
}
