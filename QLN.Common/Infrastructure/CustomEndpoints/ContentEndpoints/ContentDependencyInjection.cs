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
                //.MapContentsDailyEndpoint()
                //.MapNewsCommunityEndpoint()
                //.MapNewsQatarEndpoint()
                //.MapNewsMiddleEastEndpoint()
                //.MapNewsWorldEndpoint()
                //.MapNewsHealthEducationEndpoint()
                //.MapNewsLawEndpoint()
                //.MapFinanceEntrepreneurshipEndpoint()
                //.MapFinanceFinanceEndpoint()
                //.MapFinanceJobsCareersEndpoint()
                //.MapFinanceMarketUpdateEndpoint()
                //.MapFinanceQatarEndpoint()
                //.MapFinanceRealEstateEndpoint()
                //.MapCommunityMorePostsEndpoint()
                //.MapGetPostBySlugEndpoint()
                //.MapContentVideosEndpoint()
                //.MapFeaturedEventsEndpoint()
                //.MapLifestyleArtsCultureEndpoint()
                //.MapLifestyleEventsEndpoint()
                //.MapLifestyleFoodDiningEndpoint()
                //.MapLifestyleFashionStyleEndpoint()
                //.MapLifestyleHomeLivingEndpoint()
                //.MapLifestyleTravelLeisureEndpoint()
                //.MapSportsAthleteFeaturesEndpoint()
                //.MapSportsFootballEndpoint()
                //.MapSportsInternationalEndpoint()
                //.MapSportsOlympicsEndpoint()
                //.MapSportsQatarSportsEndpoint()
                //.MapSportsMotorsportsEndpoint()
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
