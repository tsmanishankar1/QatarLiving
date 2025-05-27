using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.DTO_s
{
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
}
