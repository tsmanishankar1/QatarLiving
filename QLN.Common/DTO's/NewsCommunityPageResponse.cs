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
    // Unique Queue Responses for News Community

    public class QlnNewsNewsCommunity
    {
        [JsonPropertyName("qln_news_news_community_top_story")]
        public TopStory TopStory { get; set; }

        [JsonPropertyName("qln_news_news_community_more_articles")]
        public MoreArticles MoreArticles { get; set; }
    }

    public class NewsCommunityPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnNewsNewsCommunity)]
        public QlnNewsNewsCommunity QlnNewsNewsCommunity { get; set; }
    }
}
