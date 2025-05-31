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
    // Unique Queue Responses for Daily Content

    public class QlnVideos
    {
        [JsonPropertyName("qln_videos_top_videos")]
        public BaseQueueResponse<ContentPost> QlnVideosTopVideos { get; set; }

        [JsonPropertyName("qln_videos_more_videos_1")]
        public BaseQueueResponse<ContentPost> QlnVideosMoreVideos1 { get; set; }

        [JsonPropertyName("qln_videos_more_videos_2")]
        public BaseQueueResponse<ContentPost> QlnVideosMoreVideos2 { get; set; }
    }


    public class ContentsVideosResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnContentVideos)]
        public QlnVideos QlnVideos { get; set; }
    }
}
