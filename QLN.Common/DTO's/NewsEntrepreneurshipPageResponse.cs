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

    public class QlnNewsFinanceEntrepreneurship
    {
        const string _prefix = "qln_news_finance_entrepreneurship";

        [JsonPropertyName($"qln_news_business_top_story")]
        public BaseQueueResponse<ContentPost> TopStory { get; set; }

        [JsonPropertyName($"qln_news_finance_entrepreneurship_watch_on_qatar_living")]
        public BaseQueueResponse<ContentPost> MoreArticles { get; set; }

        [JsonPropertyName($"{_prefix}_articles_1")]
        public BaseQueueResponse<ContentPost> Articles1 { get; set; }

        [JsonPropertyName($"{_prefix}_articles_2")]
        public BaseQueueResponse<ContentPost> Articles2 { get; set; }

        [JsonPropertyName($"{_prefix}_most_popular_articles")]
        public BaseQueueResponse<ContentPost> MostPopularArticles { get; set; }
    }

    public class NewsEntrepreneurshipPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnNewsFinanceEntrepreneurship)]
        public QlnNewsFinanceEntrepreneurship QlnNewsFinanceEntrepreneurship { get; set; }
    }
}


