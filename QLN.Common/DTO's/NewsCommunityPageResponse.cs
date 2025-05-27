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
    public class QlnNewsNewsCommunityTopStory : BaseQueueResponse<ContentPost>;
    public class QlnNewsNewsCommunityMoreArticles : BaseQueueResponse<ContentPost>;

    public class QlnNewsNewsCommunity
    {
        [JsonPropertyName("qln_news_news_community_top_story")]
        public QlnNewsNewsCommunityTopStory QlnNewsNewsCommunityTopStory { get; set; }

        [JsonPropertyName("qln_news_news_community_more_articles")]
        public QlnNewsNewsCommunityMoreArticles QlnNewsNewsCommunityMoreArticles { get; set; }
    }

    public class NewsCommunityPageResponse
    {
        [JsonPropertyName(ContentConstants.QlnNewsNewsCommunity)]
        public QlnNewsNewsCommunity QlnNewsNewsCommunity { get; set; }
    }
}
