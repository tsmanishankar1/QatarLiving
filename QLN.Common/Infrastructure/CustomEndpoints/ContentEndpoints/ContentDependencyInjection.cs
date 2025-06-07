using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ContentEndpoints
{
    public static class ContentEndPointDependency
    {

        public static RouteGroupBuilder MapContentLandingEndpoints(this RouteGroupBuilder group)
        {
            group
                .MapContentEventsEndpoint()
                .MapContentCategoriesEndpoint()
                .MapContentCommunityEndpoint()
                .MapContentQueueEndpoint()
                .MapGetPostBySlugEndpoint()
                .MapGetEventBySlugEndpoint()
                .MapGetNewsBySlugEndpoint()
                .MapPostCommentEndpoint()
                .MapPostDiscussionPostEndpoint()
                .MapChangePostLikeStatusEndpoint()
                .MapChangeCommentLikeStatusEndpoint()
                .MapContentGetCommentsEndpoint()
                ;

            return group;
        }
    }
}
