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
                .MapContentsDailyEndpoint()
                .MapNewsCommunityEndpoint()
                .MapNewsQatarEndpoint()
                .MapNewsMiddleEastEndpoint()
                .MapNewsWorldEndpoint()
                .MapNewsHealthEducationEndpoint()
                .MapNewsLawEndpoint()
                .MapFinanceEntrepreneurshipEndpoint()
                .MapFinanceFinanceEndpoint()
                .MapFinanceJobsCareersEndpoint()
                .MapFinanceMarketUpdateEndpoint()
                .MapFinanceQatarEndpoint()
                .MapFinanceRealEstateEndpoint()
                .MapContentQueueEndpoint()
                .MapCommunityMorePostsEndpoint()
                .MapGetPostBySlugEndpoint()
                .MapGetEventBySlugEndpoint()
                .MapGetNewsBySlugEndpoint()
                .MapPostCommentEndpoint()
                .MapPostForumPostEndpoint()
                .MapChangePostLikeStatusEndpoint()
                .MapChangeCommentLikeStatusEndpoint()
                .MapContentGetCommentsEndpoint()
                .MapContentVideosEndpoint()
                .MapFeaturedEventsEndpoint();

            return group;
        }
    }
}
