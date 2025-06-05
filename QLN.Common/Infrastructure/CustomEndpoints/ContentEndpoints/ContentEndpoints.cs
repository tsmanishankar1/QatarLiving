using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.Model;
using System;
using System.Linq;

namespace QLN.Common.Infrastructure.CustomEndpoints.ContentEndpoints
{
    public static class ContentEndpoints
    {
        private const int CATEGORY_CACHE_EXPIRY_IN_MINS = 60;
        private const int NEWS_CACHE_EXPIRY_IN_MINS = 5;
        private const int COMMUNITY_CACHE_EXPIRY_IN_MINS = 1;
        private const int EVENTS_CACHE_EXPIRY_IN_MINS = 10;

        #region All News
        public static RouteGroupBuilder MapGetNewsBySlugEndpoint(this RouteGroupBuilder group)
        {


            // GET /api/content/news/{slug}
            group.MapGet("/news/{*slug}", async (
                    HttpContext context,
                    [FromRoute] string slug,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var ad = await svc.GetNewsBySlugAsync(slug, cancellationToken);
                    if (ad is null)
                    {
                        return Results.NotFound();
                    }
                    else
                    {
                        // Only Cache successful results
                        AddCachingToHeader(context, NEWS_CACHE_EXPIRY_IN_MINS);
                        return Results.Ok(ad);
                    }
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetNewsBySlug")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapContentCategoriesEndpoint(this RouteGroupBuilder group)
        {
            // GET /api/content/categories
            group.MapGet("/categories", async (
                    HttpContext context,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {

                    var model = await svc.GetCategoriesFromDrupalAsync(cancellationToken);

                    AddCachingToHeader(context, CATEGORY_CACHE_EXPIRY_IN_MINS);

                    return Results.Ok(model);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Content Categories Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetContentCategories")
            .WithTags("Content");

            return group;
        }


        #endregion
        #region Content Daily
        //public static RouteGroupBuilder MapContentsDailyEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<ContentsDailyPageResponse>(DrupalContentConstants.QlnContentsDaily, "GetContentsDaily");

        //    return group;
        //}


        #endregion
        #region Featured Events
        //public static RouteGroupBuilder MapFeaturedEventsEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnFeaturedEventsPageResponse>(DrupalContentConstants.QlnFeaturedEvents, "GetFeaturedEvents");

        //    return group;
        //}


        #endregion
        #region News
        /// <summary>
        /// Maps Content endpoints: detail and landing.
        /// </summary>
        //public static RouteGroupBuilder MapNewsCommunityEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsNewsCommunityPageResponse>(DrupalContentConstants.QlnNewsNewsCommunity, "GetNewsCommunity");

        //    return group;
        //}

        //public static RouteGroupBuilder MapNewsMiddleEastEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsNewsMiddleEastPageResponse>(DrupalContentConstants.QlnNewsNewsMiddleEast, "GetNewsMiddleEast");

        //    return group;
        //}

        //public static RouteGroupBuilder MapNewsWorldEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsNewsWorldPageResponse>(DrupalContentConstants.QlnNewsNewsWorld, "GetNewsWorld");

        //    return group;
        //}

        //public static RouteGroupBuilder MapNewsHealthEducationEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsNewsHealthEducationPageResponse>(DrupalContentConstants.QlnNewsNewsHealthEducation, "GetNewsHealthEducation");

        //    return group;
        //}

        //public static RouteGroupBuilder MapNewsLawEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsNewsLawPageResponse>(DrupalContentConstants.QlnNewsNewsLaw, "GetNewsLaw");

        //    return group;
        //}

        //public static RouteGroupBuilder MapNewsQatarEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsNewsQatarPageResponse>(DrupalContentConstants.QlnNewsNewsQatar, "GetNewsQatar");

        //    return group;

        //}
        #endregion
        #region Community

        public static RouteGroupBuilder MapPostDiscussionPostEndpoint(this RouteGroupBuilder group)
        {


            // GET /api/content/post/save
            group.MapPost("/post/save", async (
                    [FromBody] CreateDiscussionPostRequest request,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var comment = await svc.CreateDiscussionPostOnDrupalAsync(request, cancellationToken);
                    return comment is null ? Results.NotFound() : Results.Ok(comment);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Create Post Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("PostForumPost")
            .WithTags("Content");

            return group;
        }

        //public static RouteGroupBuilder MapCommunityMorePostsEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<CommunityMorePostsResponse>(DrupalContentConstants.QlnCommunityMorePosts, "GetCommunityMorePosts");

        //    return group;

        //}
        public static RouteGroupBuilder MapContentCommunityEndpoint(this RouteGroupBuilder group)
        {
            // GET /api/content/categories
            group.MapGet("/community", async (
                    [FromQuery] string? forum_id,
                    [FromQuery] string? order,
                    [FromQuery] int? page,
                    [FromQuery] int? page_size,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var model = await svc.GetCommunitiesFromDrupalAsync(
                        cancellationToken,
                        forum_id,
                        order,
                        page,
                        page_size);

                    return Results.Ok(model);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Content Communities Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetContentCommunities")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapGetPostBySlugEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/event/{slug}
            group.MapGet("/post/{*slug}", async (
                    HttpContext context,
                    [FromRoute] string slug,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var ad = await svc.GetPostBySlugAsync(slug, cancellationToken);
                    if (ad is null)
                    {
                        return Results.NotFound();
                    }
                    else
                    {
                        // Only Cache successful results
                        AddCachingToHeader(context, COMMUNITY_CACHE_EXPIRY_IN_MINS);
                        return Results.Ok(ad);
                    }
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetPostBySlug")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapContentGetCommentsEndpoint(this RouteGroupBuilder group)
        {
            // GET /api/content/categories
            group.MapGet("/comments/{article_id}", async (
                    HttpContext context,
                    [FromRoute] string article_id,
                    [FromQuery] int? page,
                    [FromQuery] int? page_size,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var model = await svc.GetCommentsFromDrupalAsync(
                        article_id,
                        cancellationToken,
                        page,
                        page_size);

                    return Results.Ok(model);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Content Categories Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetContentComments")
            .WithTags("Content");

            return group;
        }

        #endregion
        #region Finance
        public static RouteGroupBuilder MapFinanceEntrepreneurshipEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<QlnNewsFinanceEntrepreneurshipPageResponse>(DrupalContentConstants.QlnNewsFinanceEntrepreneurship, "GetFinanceEntrepreneurship");

            return group;

        }

        //public static RouteGroupBuilder MapFinanceFinanceEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsFinanceFinancePageResponse>(DrupalContentConstants.QlnNewsFinanceFinance, "GetFinanceFinance");

        //    return group;

        //}

        //public static RouteGroupBuilder MapFinanceJobsCareersEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsFinanceJobsCareersPageResponse>(DrupalContentConstants.QlnNewsFinanceJobsCareers, "GetFinanceJobsCareers");

        //    return group;

        //}

        //public static RouteGroupBuilder MapFinanceMarketUpdateEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsFinanceMarketUpdatePageResponse>(DrupalContentConstants.QlnNewsFinanceMarketUpdate, "GetFinanceMarketUpdate");

        //    return group;

        //}

        //public static RouteGroupBuilder MapFinanceQatarEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsFinanceQatarPageResponse>(DrupalContentConstants.QlnNewsFinanceQatar, "GetFinanceQatar");

        //    return group;

        //}

        //public static RouteGroupBuilder MapFinanceRealEstateEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsFinanceRealEstatePageResponse>(DrupalContentConstants.QlnNewsFinanceRealEstate, "GetFinanceRealEstate");

        //    return group;

        //}
        #endregion
        #region Lifestyle
        //public static RouteGroupBuilder MapLifestyleArtsCultureEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsLifestyleArtsCulturePageResponse>(DrupalContentConstants.QlnNewsLifestyleArtsCulture, "GetLifestyleArtsCulture");

        //    return group;

        //}

        //public static RouteGroupBuilder MapLifestyleEventsEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsLifestyleEventsPageResponse>(DrupalContentConstants.QlnNewsLifestyleEvents, "GetLifestyleEvents");

        //    return group;

        //}

        //public static RouteGroupBuilder MapLifestyleFoodDiningEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsLifestyleFoodDiningPageResponse>(DrupalContentConstants.QlnNewsLifestyleFoodDining, "GetLifestyleFoodDining");

        //    return group;

        //}

        //public static RouteGroupBuilder MapLifestyleTravelLeisureEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsLifestyleTravelLeisurePageResponse>(DrupalContentConstants.QlnNewsLifestyleTravelLeisure, "GetLifestyleTravelLeisure");

        //    return group;

        //}

        //public static RouteGroupBuilder MapLifestyleHomeLivingEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsLifestyleHomeLivingPageResponse>(DrupalContentConstants.QlnNewsLifestyleHomeLiving, "GetLifestyleHomeLiving");

        //    return group;

        //}

        //public static RouteGroupBuilder MapLifestyleFashionStyleEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsLifestyleFashionStylePageResponse>(DrupalContentConstants.QlnNewsLifestyleFashionStyle, "GetLifestyleFashionStyle");

        //    return group;

        //}
        #endregion
        #region Sports
        //public static RouteGroupBuilder MapSportsAthleteFeaturesEndpoint(this RouteGroupBuilder group)
        //{
        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsSportsAthleteFeaturesPageResponse>(DrupalContentConstants.QlnNewsSportsAthleteFeatures, "GetSportsAthleteFeatures");
        //    return group;
        //}

        //public static RouteGroupBuilder MapSportsFootballEndpoint(this RouteGroupBuilder group)
        //{
        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsSportsFootballPageResponse>(DrupalContentConstants.QlnNewsSportsFootball, "GetSportsFootball");
        //    return group;
        //}

        //public static RouteGroupBuilder MapSportsInternationalEndpoint(this RouteGroupBuilder group)
        //{
        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsSportsInternationalPageResponse>(DrupalContentConstants.QlnNewsSportsInternational, "GetSportsInternational");
        //    return group;
        //}

        //public static RouteGroupBuilder MapSportsMotorsportsEndpoint(this RouteGroupBuilder group)
        //{
        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsSportsMotorsportsPageResponse>(DrupalContentConstants.QlnNewsSportsMotorsports, "GetSportsMotorsports");
        //    return group;
        //}

        //public static RouteGroupBuilder MapSportsOlympicsEndpoint(this RouteGroupBuilder group)
        //{
        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsSportsOlympicsPageResponse>(DrupalContentConstants.QlnNewsSportsOlympics, "GetSportsOlympics");
        //    return group;
        //}

        //public static RouteGroupBuilder MapSportsQatarSportsEndpoint(this RouteGroupBuilder group)
        //{
        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<QlnNewsSportsQatarSportsPageResponse>(DrupalContentConstants.QlnNewsSportsQatarSports, "GetSportsQatarSports");
        //    return group;
        //}

        #endregion
        #region Videos
        //public static RouteGroupBuilder MapContentVideosEndpoint(this RouteGroupBuilder group)
        //{

        //    // GET /api/content/landing
        //    group.GenerateLandingEndpoint<ContentsVideosResponse>(DrupalContentConstants.QlnContentVideos, "GetVideos");

        //    return group;

        //}
        #endregion
        #region Support endpoints
        public static RouteGroupBuilder MapContentQueueEndpoint(this RouteGroupBuilder group)
        {
            // dont add caching to this as it is used for testing

            // GET /api/content/landing
            group.MapGet("{queue}/landing", async (
                    [FromRoute] string queue,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var response = new GeneralNewsResponse
                    {
                        News = new GenericNewsPageResponse()
                    };

                    switch (queue)
                    {
                        // Finance
                        case DrupalContentConstants.QlnNewsFinanceEntrepreneurship:
                            var newsFinanceEntrepreneurship = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceEntrepreneurshipPageResponse>(queue, cancellationToken);
                            if (newsFinanceEntrepreneurship != null) response = (GeneralNewsResponse)newsFinanceEntrepreneurship;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceFinance:
                            var newsFinanceFinance = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceFinancePageResponse>(queue, cancellationToken);
                            if (newsFinanceFinance != null) response = (GeneralNewsResponse)newsFinanceFinance;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceJobsCareers:
                            var newsFinanceJobsCareers = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceJobsCareersPageResponse>(queue, cancellationToken);
                            if (newsFinanceJobsCareers != null) response = (GeneralNewsResponse)newsFinanceJobsCareers;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceMarketUpdate:
                            var newsFinanceMarketUpdate = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceMarketUpdatePageResponse>(queue, cancellationToken);
                            if (newsFinanceMarketUpdate != null) response = (GeneralNewsResponse)newsFinanceMarketUpdate;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceQatar:
                            var newsFinanceQatar = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceQatarPageResponse>(queue, cancellationToken);
                            if (newsFinanceQatar != null) response = (GeneralNewsResponse)newsFinanceQatar;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceRealEstate:
                            var newsFinanceRealEstate = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceRealEstatePageResponse>(queue, cancellationToken);
                            if (newsFinanceRealEstate != null) response = (GeneralNewsResponse)newsFinanceRealEstate;
                            break;
                        // News
                        case DrupalContentConstants.QlnNewsNewsCommunity:
                            var newsNewsCommunity = await svc.GetPostsFromDrupalAsync<QlnNewsNewsCommunityPageResponse>(queue, cancellationToken);
                            if (newsNewsCommunity != null) response = (GeneralNewsResponse)newsNewsCommunity;
                            break;
                        case DrupalContentConstants.QlnNewsNewsMiddleEast:
                            var newsNewsMiddleEast = await svc.GetPostsFromDrupalAsync<QlnNewsNewsMiddleEastPageResponse>(queue, cancellationToken);
                            if (newsNewsMiddleEast != null) response = (GeneralNewsResponse)newsNewsMiddleEast;
                            break;
                        case DrupalContentConstants.QlnNewsNewsWorld:
                            var newsNewsWorld = await svc.GetPostsFromDrupalAsync<QlnNewsNewsWorldPageResponse>(queue, cancellationToken);
                            if (newsNewsWorld != null) response = (GeneralNewsResponse)newsNewsWorld;
                            break;
                        case DrupalContentConstants.QlnNewsNewsHealthEducation:
                            var newsNewsHealthEducation = await svc.GetPostsFromDrupalAsync<QlnNewsNewsHealthEducationPageResponse>(queue, cancellationToken);
                            if (newsNewsHealthEducation != null) response = (GeneralNewsResponse)newsNewsHealthEducation;
                            break;
                        case DrupalContentConstants.QlnNewsNewsLaw:
                            var newsNewsLaw = await svc.GetPostsFromDrupalAsync<QlnNewsNewsLawPageResponse>(queue, cancellationToken);
                            if (newsNewsLaw != null) response = (GeneralNewsResponse)newsNewsLaw;
                            break;
                        case DrupalContentConstants.QlnNewsNewsQatar:
                            var newsNewsQatar = await svc.GetPostsFromDrupalAsync<QlnNewsNewsQatarPageResponse>(queue, cancellationToken);
                            if (newsNewsQatar != null) response = (GeneralNewsResponse)newsNewsQatar;
                            break;
                        // Lifestyle
                        case DrupalContentConstants.QlnNewsLifestyleArtsCulture:
                            var newsLifestyleArtsCulture = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleArtsCulturePageResponse>(queue, cancellationToken);
                            if (newsLifestyleArtsCulture != null) response = (GeneralNewsResponse)newsLifestyleArtsCulture;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleEvents:
                            var newsLifestyleEvents = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleEventsPageResponse>(queue, cancellationToken);
                            if (newsLifestyleEvents != null) response = (GeneralNewsResponse)newsLifestyleEvents;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleFoodDining:
                            var newsLifestyleFoodDining = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleFoodDiningPageResponse>(queue, cancellationToken);
                            if (newsLifestyleFoodDining != null) response = (GeneralNewsResponse)newsLifestyleFoodDining;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleTravelLeisure:
                            var newsLifestyleTravelLeisure = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleTravelLeisurePageResponse>(queue, cancellationToken);
                            if (newsLifestyleTravelLeisure != null) response = (GeneralNewsResponse)newsLifestyleTravelLeisure;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleHomeLiving:
                            var newsLifestyleHomeLiving = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleHomeLivingPageResponse>(queue, cancellationToken);
                            if (newsLifestyleHomeLiving != null) response = (GeneralNewsResponse)newsLifestyleHomeLiving;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleFashionStyle:
                            var newsLifestyleFashionStyle = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleFashionStylePageResponse>(queue, cancellationToken);
                            if (newsLifestyleFashionStyle != null) response = (GeneralNewsResponse)newsLifestyleFashionStyle;
                            break;
                        // Sports
                        case DrupalContentConstants.QlnNewsSportsAthleteFeatures:
                            var newsSportsAthleteFeatures = await svc.GetPostsFromDrupalAsync<QlnNewsSportsAthleteFeaturesPageResponse>(queue, cancellationToken);
                            if (newsSportsAthleteFeatures != null) response = (GeneralNewsResponse)newsSportsAthleteFeatures;
                            break;
                        case DrupalContentConstants.QlnNewsSportsFootball:
                            var newsSportsFootball = await svc.GetPostsFromDrupalAsync<QlnNewsSportsFootballPageResponse>(queue, cancellationToken);
                            if (newsSportsFootball != null) response = (GeneralNewsResponse)newsSportsFootball;
                            break;
                        case DrupalContentConstants.QlnNewsSportsInternational:
                            var newsSportsInternational = await svc.GetPostsFromDrupalAsync<QlnNewsSportsInternationalPageResponse>(queue, cancellationToken);
                            if (newsSportsInternational != null) response = (GeneralNewsResponse)newsSportsInternational;
                            break;
                        case DrupalContentConstants.QlnNewsSportsMotorsports:
                            var newsSportsMotorsports = await svc.GetPostsFromDrupalAsync<QlnNewsSportsMotorsportsPageResponse>(queue, cancellationToken);
                            if (newsSportsMotorsports != null) response = (GeneralNewsResponse)newsSportsMotorsports;
                            break;
                        case DrupalContentConstants.QlnNewsSportsOlympics:
                            var newsSportsOlympics = await svc.GetPostsFromDrupalAsync<QlnNewsSportsOlympicsPageResponse>(queue, cancellationToken);
                            if (newsSportsOlympics != null) response = (GeneralNewsResponse)newsSportsOlympics;
                            break;
                        case DrupalContentConstants.QlnNewsSportsQatarSports:
                            var newsSportsQatarSports = await svc.GetPostsFromDrupalAsync<QlnNewsSportsQatarSportsPageResponse>(queue, cancellationToken);
                            if (newsSportsQatarSports != null) response = (GeneralNewsResponse)newsSportsQatarSports;
                            break;
                        // Things below do not map to a news response
                        // Videos
                        case DrupalContentConstants.QlnContentVideos:
                            var contentVideos = await svc.GetPostsFromDrupalAsync<ContentsVideosResponse>(queue, cancellationToken);
                            if (contentVideos != null) return Results.Ok(contentVideos);
                            break;
                        // Daily
                        case DrupalContentConstants.QlnContentsDaily:
                            var contentsDaily = await svc.GetPostsFromDrupalAsync<ContentsDailyPageResponse>(queue, cancellationToken);
                            if (contentsDaily != null) return Results.Ok(contentsDaily);
                            break;
                        // Featured Events
                        case DrupalContentConstants.QlnFeaturedEvents:
                            var featuredEvents = await svc.GetPostsFromDrupalAsync<QlnFeaturedEventsPageResponse>(queue, cancellationToken);
                            if (featuredEvents != null) return Results.Ok(featuredEvents);
                            break;
                        // Community More Posts
                        case DrupalContentConstants.QlnCommunityMorePosts:
                            var communityMorePosts = await svc.GetPostsFromDrupalAsync<CommunityMorePostsResponse>(queue, cancellationToken);
                            if (communityMorePosts != null) return Results.Ok(communityMorePosts);
                            break;
                        default:
                            // return with an empty response
                            break;
                    }
                    
                    return Results.Ok(response);

                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Landing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetLandingByQueue")
            .WithDescription("Tester method for testing out as yet unmapped Drupal queues - returns a dynamic object so isnt very efficient")
            .WithTags("Content");

            return group;
        }
        #endregion
        #region Events
        public static RouteGroupBuilder MapContentEventsEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/events
            group.MapGet("/events", async (
                    HttpContext context,
                    [FromQuery] string? category_id,
                    [FromQuery] string? location_id,
                    [FromQuery] string? from,
                    [FromQuery] string? to,
                    [FromQuery] string? order,
                    [FromQuery] int? page,
                    [FromQuery] int? page_size,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var model = await svc.GetEventsFromDrupalAsync(
                        cancellationToken,
                        category_id,
                        location_id,
                        from,
                        to,
                        order,
                        page,
                        page_size);

                    AddCachingToHeader(context, EVENTS_CACHE_EXPIRY_IN_MINS);

                    return Results.Ok(model);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Content Events Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetContentEvents")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapGetEventBySlugEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/event/{slug}
            group.MapGet("/event/{*slug}", async (
                    HttpContext context,
                    [FromRoute] string slug,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var ad = await svc.GetEventBySlugAsync(slug, cancellationToken);
                    if (ad is null)
                    {
                        return Results.NotFound();
                    }
                    else
                    {
                        // Only Cache successful results
                        AddCachingToHeader(context, EVENTS_CACHE_EXPIRY_IN_MINS);

                        return Results.Ok(ad);
                    }
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetEventBySlug")
            .WithTags("Content");

            return group;
        }

        #endregion
        #region Categories
        #endregion

        public static RouteGroupBuilder MapPostCommentEndpoint(this RouteGroupBuilder group)
        {


            // GET /api/content/comment/save
            group.MapPost("/comment/save", async (
                    [FromBody] CreateCommentRequest request,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var comment = await svc.CreateCommentOnDrupalAsync(request, cancellationToken);
                    return comment is null ? Results.NotFound() : Results.Ok(comment);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Create Comment Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("PostComment")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapChangeCommentLikeStatusEndpoint(this RouteGroupBuilder group)
        {
            // GET /api/content/like
            group.MapGet("/like/post/{articleId}/{userId}/{action}", async (
                    //[FromBody] ChangeLikeStatusRequest request,
                    [FromRoute] int articleId,
                    [FromRoute] int userId,
                    [FromRoute] string action,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                if (action != "like" && action != "unlike")
                    return Results.BadRequest("action can only be like or unlike");

                var request = new ChangeLikeStatusRequest
                {
                    Uid = userId,
                    Nid = articleId,
                    Action = action
                };

                try
                {
                    var comment = await svc.ChangeLikeStatusOnDrupalAsync(request, cancellationToken);
                    return comment is null ? Results.NotFound() : Results.Ok(comment);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Change Post Like Status Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("PostChangePostLikeStatus")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapChangePostLikeStatusEndpoint(this RouteGroupBuilder group)
        {
            // GET /api/content/like
            group.MapGet("/like/comment/{articleId}/{userId}/{action}", async (
                    //[FromBody] ChangeLikeStatusRequest request,
                    [FromRoute] int articleId,
                    [FromRoute] int userId,
                    [FromRoute] string action,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                if (action != "like" && action != "unlike") 
                    return Results.BadRequest("action can only be like or unlike");

                var request = new ChangeLikeStatusRequest
                {
                    Uid = userId,
                    Nid = articleId,
                    Action = action
                };

                try
                {
                    var comment = await svc.ChangeLikeStatusOnDrupalAsync(request, cancellationToken);
                    return comment is null ? Results.NotFound() : Results.Ok(comment);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Change Comment Like Status Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("PostChangeCommentLikeStatus")
            .WithTags("Content");

            return group;
        }

        #region Private endpoints

        private static RouteGroupBuilder GenerateLandingEndpoint<T>(this RouteGroupBuilder group, string QueueName, string Name)
        {

            // GET /api/content/landing
            group.MapGet($"{QueueName}/landing", async (
                    HttpContext context,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var model = await svc.GetPostsFromDrupalAsync<T>(QueueName, cancellationToken);

                    //var genericResponse = new GeneralNewsResponse<T>
                    //{
                        //    News = default(T) // Initialize with default value
                    //}
                    //;

                    //if (model != null)
                    //{
                    //    genericResponse.News.TopStory = model.News.TopStory;
                    //    genericResponse.News.MoreArticles = model.News.MoreArticles;
                    //    genericResponse.News.MostPopularArticles = model.News.MostPopularArticles;
                    //    genericResponse.News.Articles1 = model.News.Articles1;
                    //    genericResponse.News.Articles2 = model.News.Articles2;
                    //    genericResponse.News.WatchOnQatarLiving = model.News.WatchOnQatarLiving;

                    //}


                    AddCachingToHeader(context, NEWS_CACHE_EXPIRY_IN_MINS);

                    return Results.Ok(model);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Landing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName(Name)
            .WithTags("Content");

            return group;
        }

        private static void AddCachingToHeader(HttpContext context, int cache_expiry_in_minutes)
        {
            context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromMinutes(cache_expiry_in_minutes)
            };

            // Add Vary header for the User-Agent
            context.Response.Headers[HeaderNames.Vary] = "User-Agent";
        }
        #endregion
    }
}

