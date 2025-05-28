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
    public class QlnNewsNewsLaw
    {
        [JsonPropertyName("qln_news_news_law_top_story")]
        public TopStory TopStory { get; set; }

        [JsonPropertyName("qln_news_news_law_more_articles")]
        public MoreArticles Articles { get; set; }
    }

    public class NewsLawPageResponse
    {
        [JsonPropertyName(ContentConstants.QlnNewsNewsLaw)]
        public QlnNewsNewsLaw QlnNewsNewsLaw { get; set; }
    }
}
