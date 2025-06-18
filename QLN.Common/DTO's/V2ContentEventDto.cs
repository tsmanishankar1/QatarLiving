using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class V2ContentEventDto
    {
        [JsonPropertyName("qln_events")]
        public QlnEventsDto QlnEvents { get; set; } = new();
    }

    public class QlnEventsDto
    {
        [JsonPropertyName("qln_events_featured_events")]
        public QlnEventsQueueDto FeaturedEvents { get; set; } = new();
    }

    public class QlnEventsQueueDto
    {
        [JsonPropertyName("queue_label")]
        public string QueueLabel { get; set; } = "Featured Events";

        [JsonPropertyName("items")]
        public List<ContentEventDto> Items { get; set; } = new();
    }
    public class ContentEventDto : ContentBase
    {
        public Guid Id { get; set; }
        public Guid User_id { get; set; }
        [JsonPropertyName("category_id")]
        public string CategroryId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("entity_organizer")]
        public string EntityOrganizer { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("event_category")]
        public string EventCategory { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("event_venue")]
        public string EventVenue { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("event_start")]
        public string EventStart { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("event_end")]
        public string EventEnd { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("event_lat")]
        public string EventLat { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("event_long")]
        public string EventLong { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("event_location")]
        public string EventLocation { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("nid")]
        public string Nid { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("date_created")]
        public string DateCreated { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("forum_id")]
        public string ForumId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("comments")]
        public List<ContentComment> Comments { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
