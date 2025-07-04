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
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("topicName")]
        public string TopicName { get; set; } = string.Empty;
    }

}
