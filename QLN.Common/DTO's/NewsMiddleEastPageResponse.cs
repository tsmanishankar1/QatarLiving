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
    // Unique Queue Responses for News Middle East

    public class QlnNewsNewsMiddleEast
    {
        [JsonPropertyName("news_news_middle_east_topstory")]
        public TopStory TopStory { get; set; }

        [JsonPropertyName("news_news_middle_east_more_articles")]
        public MoreArticles MoreArticles { get; set; }
    }

    public class NewsMiddleEastPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnNewsNewsMiddleEast)]
        public QlnNewsNewsMiddleEast QlnNewsNewsMiddleEast { get; set; }
    }
}
