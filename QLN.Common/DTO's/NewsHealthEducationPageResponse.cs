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

    public class QlnNewsNewsHealthEducation
    {
        [JsonPropertyName("news_news_health_education_top_story")]
        public TopStory TopStory { get; set; }

        [JsonPropertyName("news_news_health_education_more_articles")]
        public MoreArticles MoreArticles { get; set; }
    }

    public class NewsHealthEducationPageResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnNewsNewsHealthEducation)]
        public QlnNewsNewsHealthEducation QlnNewsNewsHealthEducation { get; set; }
    }
}
