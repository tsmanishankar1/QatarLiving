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
    // Unique Queue Responses for Events
    public class QlnEvents
    {
        const string QueuePrefix = DrupalContentConstants.QlnFeaturedEvents;


        [JsonPropertyName($"{QueuePrefix}_featured_events")]
        public BaseQueueResponse<ContentEvent> QlnEventsFeaturedEvents { get; set; }

        [JsonPropertyName($"{QueuePrefix}_watch_on_qatar_living")]
        public BaseQueueResponse<ContentVideo> WatchOnQatarLiving { get; set; }
    }

    public class QlnFeaturedEventsPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnFeaturedEvents)]
        public QlnEvents QlnEvents { get; set; }
    }
}
