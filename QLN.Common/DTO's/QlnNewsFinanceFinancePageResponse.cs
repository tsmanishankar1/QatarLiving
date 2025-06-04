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

    public class QlnNewsFinanceFinance
    {
        const string QueuePrefix = DrupalContentConstants.QlnNewsFinanceFinance;

        //[JsonPropertyName($"{QueuePrefix}_top_story")]
        [JsonPropertyName($"qln_news_business_finance_top_story")]
        public BaseQueueResponse<ContentPost> TopStory { get; set; }

        //[JsonPropertyName($"{QueuePrefix}_more_articles")]
        [JsonPropertyName($"qln_news_business_finance_more_articles")]
        public BaseQueueResponse<ContentPost> MoreArticles { get; set; }

        [JsonPropertyName($"{QueuePrefix}_articles_1")]
        public BaseQueueResponse<ContentPost> Articles1 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_articles_2")]
        public BaseQueueResponse<ContentPost> Articles2 { get; set; }

        [JsonPropertyName($"{QueuePrefix}_most_popular_articles")]
        public BaseQueueResponse<ContentPost> MostPopularArticles { get; set; }

        [JsonPropertyName($"{QueuePrefix}_watch_on_qatar_living")]
        public BaseQueueResponse<ContentVideo> WatchOnQatarLiving { get; set; }
    }

    public class QlnNewsFinanceFinancePageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnNewsFinanceFinance)]
        public QlnNewsFinanceFinance QlnNewsFinanceFinance { get; set; }
    }
}


