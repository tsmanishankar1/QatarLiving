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
    // Unique Queue Responses for Community More Posts

    public class QlnCommunityPost
    {
        [JsonPropertyName("qln_community_post_more_posts")]
        public QlnCommunityPostMorePosts QlnCommunityPostMorePosts { get; set; }
    }

    public class QlnCommunityPostMorePosts : BaseQueueResponse<ContentEvent>;

    public class CommunityMorePostsResponse
    {
        [JsonPropertyName(DrupalContentConstants.QlnCommunityMorePosts)]
        public QlnCommunityPost QlnCommunityPost { get; set; }
    }
}
