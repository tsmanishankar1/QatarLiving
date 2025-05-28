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
    // Unique Queue Responses for New Qatar

    public class QlnNewsNewsQatar
    {
        [JsonPropertyName("news_news_qatar_top_story")]
        public TopStory TopStory { get; set; }

        [JsonPropertyName("news_news_qatar_more_articles")]
        public MoreArticles MoreArticles { get; set; }
    }

    public class NewsQatarPageResponse
    {
        [JsonPropertyName(ContentConstants.QlnNewsNewsQatar)]
        public QlnNewsNewsQatar QlnNewsNewsQatar { get; set; }
    }

}
