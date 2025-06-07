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
        private const int COMMUNITY_CACHE_EXPIRY_IN_MINS = 10;
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
                        AddPathCachingToHeader(context, NEWS_CACHE_EXPIRY_IN_MINS);
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

                    AddPathCachingToHeader(context, CATEGORY_CACHE_EXPIRY_IN_MINS);

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
                        AddPathCachingToHeader(context, COMMUNITY_CACHE_EXPIRY_IN_MINS);
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
        public static RouteGroupBuilder MapContentQueueEndpoint(this RouteGroupBuilder group)
        {
            // dont add caching to this as it is used for testing

            // GET /api/content/landing
            group.MapGet("{queue}/landing", async (
                    HttpContext context,
                    [FromRoute] string queue,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var doCache = true;

                    var response = new GenericNewsPageResponse
                    {
                        News = new GenericNewsPage()
                    };

                    switch (queue)
                    {
                        // Finance
                        case DrupalContentConstants.QlnNewsFinanceEntrepreneurship:
                            var newsFinanceEntrepreneurship = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceEntrepreneurshipPageResponse>(queue, cancellationToken);
                            if (newsFinanceEntrepreneurship != null) response = (GenericNewsPageResponse)newsFinanceEntrepreneurship;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceFinance:
                            var newsFinanceFinance = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceFinancePageResponse>(queue, cancellationToken);
                            if (newsFinanceFinance != null) response = (GenericNewsPageResponse)newsFinanceFinance;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceJobsCareers:
                            var newsFinanceJobsCareers = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceJobsCareersPageResponse>(queue, cancellationToken);
                            if (newsFinanceJobsCareers != null) response = (GenericNewsPageResponse)newsFinanceJobsCareers;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceMarketUpdate:
                            var newsFinanceMarketUpdate = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceMarketUpdatePageResponse>(queue, cancellationToken);
                            if (newsFinanceMarketUpdate != null) response = (GenericNewsPageResponse)newsFinanceMarketUpdate;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceQatar:
                            var newsFinanceQatar = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceQatarPageResponse>(queue, cancellationToken);
                            if (newsFinanceQatar != null) response = (GenericNewsPageResponse)newsFinanceQatar;
                            break;
                        case DrupalContentConstants.QlnNewsFinanceRealEstate:
                            var newsFinanceRealEstate = await svc.GetPostsFromDrupalAsync<QlnNewsFinanceRealEstatePageResponse>(queue, cancellationToken);
                            if (newsFinanceRealEstate != null) response = (GenericNewsPageResponse)newsFinanceRealEstate;
                            break;
                        // News
                        case DrupalContentConstants.QlnNewsNewsCommunity:
                            var newsNewsCommunity = await svc.GetPostsFromDrupalAsync<QlnNewsNewsCommunityPageResponse>(queue, cancellationToken);
                            if (newsNewsCommunity != null) response = (GenericNewsPageResponse)newsNewsCommunity;
                            break;
                        case DrupalContentConstants.QlnNewsNewsMiddleEast:
                            var newsNewsMiddleEast = await svc.GetPostsFromDrupalAsync<QlnNewsNewsMiddleEastPageResponse>(queue, cancellationToken);
                            if (newsNewsMiddleEast != null) response = (GenericNewsPageResponse)newsNewsMiddleEast;
                            break;
                        case DrupalContentConstants.QlnNewsNewsWorld:
                            var newsNewsWorld = await svc.GetPostsFromDrupalAsync<QlnNewsNewsWorldPageResponse>(queue, cancellationToken);
                            if (newsNewsWorld != null) response = (GenericNewsPageResponse)newsNewsWorld;
                            break;
                        case DrupalContentConstants.QlnNewsNewsHealthEducation:
                            var newsNewsHealthEducation = await svc.GetPostsFromDrupalAsync<QlnNewsNewsHealthEducationPageResponse>(queue, cancellationToken);
                            if (newsNewsHealthEducation != null) response = (GenericNewsPageResponse)newsNewsHealthEducation;
                            break;
                        case DrupalContentConstants.QlnNewsNewsLaw:
                            var newsNewsLaw = await svc.GetPostsFromDrupalAsync<QlnNewsNewsLawPageResponse>(queue, cancellationToken);
                            if (newsNewsLaw != null) response = (GenericNewsPageResponse)newsNewsLaw;
                            break;
                        case DrupalContentConstants.QlnNewsNewsQatar:
                            var newsNewsQatar = await svc.GetPostsFromDrupalAsync<QlnNewsNewsQatarPageResponse>(queue, cancellationToken);
                            if (newsNewsQatar != null) response = (GenericNewsPageResponse)newsNewsQatar;
                            break;
                        // Lifestyle
                        case DrupalContentConstants.QlnNewsLifestyleArtsCulture:
                            var newsLifestyleArtsCulture = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleArtsCulturePageResponse>(queue, cancellationToken);
                            if (newsLifestyleArtsCulture != null) response = (GenericNewsPageResponse)newsLifestyleArtsCulture;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleEvents:
                            var newsLifestyleEvents = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleEventsPageResponse>(queue, cancellationToken);
                            if (newsLifestyleEvents != null) response = (GenericNewsPageResponse)newsLifestyleEvents;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleFoodDining:
                            var newsLifestyleFoodDining = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleFoodDiningPageResponse>(queue, cancellationToken);
                            if (newsLifestyleFoodDining != null) response = (GenericNewsPageResponse)newsLifestyleFoodDining;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleTravelLeisure:
                            var newsLifestyleTravelLeisure = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleTravelLeisurePageResponse>(queue, cancellationToken);
                            if (newsLifestyleTravelLeisure != null) response = (GenericNewsPageResponse)newsLifestyleTravelLeisure;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleHomeLiving:
                            var newsLifestyleHomeLiving = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleHomeLivingPageResponse>(queue, cancellationToken);
                            if (newsLifestyleHomeLiving != null) response = (GenericNewsPageResponse)newsLifestyleHomeLiving;
                            break;
                        case DrupalContentConstants.QlnNewsLifestyleFashionStyle:
                            var newsLifestyleFashionStyle = await svc.GetPostsFromDrupalAsync<QlnNewsLifestyleFashionStylePageResponse>(queue, cancellationToken);
                            if (newsLifestyleFashionStyle != null) response = (GenericNewsPageResponse)newsLifestyleFashionStyle;
                            break;
                        // Sports
                        case DrupalContentConstants.QlnNewsSportsAthleteFeatures:
                            var newsSportsAthleteFeatures = await svc.GetPostsFromDrupalAsync<QlnNewsSportsAthleteFeaturesPageResponse>(queue, cancellationToken);
                            if (newsSportsAthleteFeatures != null) response = (GenericNewsPageResponse)newsSportsAthleteFeatures;
                            break;
                        case DrupalContentConstants.QlnNewsSportsFootball:
                            var newsSportsFootball = await svc.GetPostsFromDrupalAsync<QlnNewsSportsFootballPageResponse>(queue, cancellationToken);
                            if (newsSportsFootball != null) response = (GenericNewsPageResponse)newsSportsFootball;
                            break;
                        case DrupalContentConstants.QlnNewsSportsInternational:
                            var newsSportsInternational = await svc.GetPostsFromDrupalAsync<QlnNewsSportsInternationalPageResponse>(queue, cancellationToken);
                            if (newsSportsInternational != null) response = (GenericNewsPageResponse)newsSportsInternational;
                            break;
                        case DrupalContentConstants.QlnNewsSportsMotorsports:
                            var newsSportsMotorsports = await svc.GetPostsFromDrupalAsync<QlnNewsSportsMotorsportsPageResponse>(queue, cancellationToken);
                            if (newsSportsMotorsports != null) response = (GenericNewsPageResponse)newsSportsMotorsports;
                            break;
                        case DrupalContentConstants.QlnNewsSportsOlympics:
                            var newsSportsOlympics = await svc.GetPostsFromDrupalAsync<QlnNewsSportsOlympicsPageResponse>(queue, cancellationToken);
                            if (newsSportsOlympics != null) response = (GenericNewsPageResponse)newsSportsOlympics;
                            break;
                        case DrupalContentConstants.QlnNewsSportsQatarSports:
                            var newsSportsQatarSports = await svc.GetPostsFromDrupalAsync<QlnNewsSportsQatarSportsPageResponse>(queue, cancellationToken);
                            if (newsSportsQatarSports != null) response = (GenericNewsPageResponse)newsSportsQatarSports;
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
                            doCache = false;
                            break;
                    }

                    if(doCache) 
                        AddPathCachingToHeader(context, COMMUNITY_CACHE_EXPIRY_IN_MINS);

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

                    AddFullCachingToHeader(context, EVENTS_CACHE_EXPIRY_IN_MINS);

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
                        AddPathCachingToHeader(context, EVENTS_CACHE_EXPIRY_IN_MINS);

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


                    AddPathCachingToHeader(context, NEWS_CACHE_EXPIRY_IN_MINS);

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

        private static void AddPathCachingToHeader(HttpContext context, int cache_expiry_in_minutes)
        {
            context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromMinutes(cache_expiry_in_minutes)
            };

            // Add Vary header for the User-Agent
            context.Response.Headers[HeaderNames.Vary] = "Accept, Accept-Encoding";

            // Add a custom header to help downstream caches (if any) to distinguish by URL
            context.Response.Headers["X-Cache-Key"] = context.Request.Path + context.Request.QueryString;
        }

        private static void AddFullCachingToHeader(HttpContext context, int cache_expiry_in_minutes)
        {
            context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromMinutes(cache_expiry_in_minutes)
            };

            // Add Vary header for query string and path to ensure unique cache per request URL
            // "Vary: Accept, Accept-Encoding" is common, but for query/path, use "Vary: *" or custom logic
            // Here, we use "Vary: Accept, Accept-Encoding" and "Vary: *" to indicate all request headers/params
            // But "Vary: *" disables caching by shared caches, so instead, use a custom header to indicate query
            // The best practice is to use the full URL as the cache key on the server side, but for HTTP headers:
            //context.Response.Headers[HeaderNames.Vary] = "Accept, Accept-Encoding";
            context.Response.Headers[HeaderNames.Vary] = "*";

            // Add a custom header to help downstream caches (if any) to distinguish by URL
            context.Response.Headers["X-Cache-Key"] = context.Request.Path + context.Request.QueryString;
        }
        #endregion
    }
}

