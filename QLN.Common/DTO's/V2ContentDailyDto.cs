using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class DailyTopic
    {
        public Guid Id { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public string IsActive { get; set; }
    }
    public enum DailySlotType
    {
        TopStory = 1,
        HighlightedEvent = 2,
        Article1 = 3,
        Article2 = 4,
        Article3 = 5,
        Article4 = 6,
        Article5 = 7,
        Article6 = 8,
        Article7 = 9
    }
    public enum DailyContentType
    {
        Article = 1,
        Event = 2,
        Video = 3
    }
    public class DailyTopSectionSlot
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        public DailySlotType SlotType { get; set; } 
        public string Title { get; set; } 
        public string Category { get; set; }
        public string? Subcategory { get; set; } 
        public Guid RelatedContentId { get; set; } 
        public DailyContentType ContentType { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int SlotNumber { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class DailyTopicContent
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public Guid TopicId { get; set; }
        public DailyContentType ContentType { get; set; }
        public Guid? RelatedContentId { get; set; }
        public string Category { get; set; }
        public string? Subcategory { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsPublished { get; set; }
        public bool IsExpired { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("topicName")]
        public string TopicName { get; set; } = string.Empty;
    }

}
