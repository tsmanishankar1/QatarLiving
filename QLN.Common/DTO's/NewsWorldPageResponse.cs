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
    // Unique Queue Responses for News World

    public class QlnNewsNewsWorld
    {
        [JsonPropertyName("news_news_world_top_story")]
        public TopStory TopStory { get; set; }

        [JsonPropertyName("news_news_world_more_articles")]
        public MoreArticles MoreArticles { get; set; }
    }

    public class NewsWorldPageResponse
    {
        [JsonPropertyName(ContentConstants.QlnNewsNewsWorld)]
        public QlnNewsNewsWorld QlnNewsNewsWorld { get; set; }
    }

}
