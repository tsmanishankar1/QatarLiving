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

    public class QlnNewsFinanceRealEstate
    {
        const string QueuePrefix = DrupalContentConstants.QlnNewsFinanceRealEstate;

        [JsonPropertyName($"{QueuePrefix}_top_story")]
        public BaseQueueResponse<ContentPost> TopStory { get; set; }

        [JsonPropertyName($"{QueuePrefix}_more_articles")]
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

    public class QlnNewsFinanceRealEstatePageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnNewsFinanceRealEstate)]
        public QlnNewsFinanceRealEstate News { get; set; }

        public static explicit operator GenericNewsPageResponse(QlnNewsFinanceRealEstatePageResponse source)
        {
            // bring back an empty object if this exists
            if (source.News == null) return new GenericNewsPageResponse
            {
                News = new GenericNewsPage()
            };

            return new GenericNewsPageResponse
            {
                News = new GenericNewsPage()
                {
                    TopStory = source.News.TopStory,
                    Articles1 = source.News.Articles1,
                    Articles2 = source.News.Articles2,
                    MoreArticles = source.News.MoreArticles,
                    MostPopularArticles = source.News.MostPopularArticles,
                    WatchOnQatarLiving = source.News.WatchOnQatarLiving
                }
            };
        }
    }
}


