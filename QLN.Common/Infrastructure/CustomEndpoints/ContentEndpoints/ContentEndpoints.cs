using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
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
        /// <summary>
        /// Maps Content endpoints: detail and landing.
        /// </summary>
        public static RouteGroupBuilder MapContentsDailyEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<ContentsDailyPageResponse>(DrupalContentConstants.QlnContentsDaily, "GetContentsDaily"); 

            return group;
        }

        public static RouteGroupBuilder MapNewsCommunityEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsCommunityPageResponse>(DrupalContentConstants.QlnNewsNewsCommunity, "GetNewsCommunity");

            return group;
        }

        public static RouteGroupBuilder MapNewsMiddleEastEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsMiddleEastPageResponse>(DrupalContentConstants.QlnNewsNewsMiddleEast, "GetNewsMiddleEast");

            return group;
        }

        public static RouteGroupBuilder MapNewsWorldEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsWorldPageResponse>(DrupalContentConstants.QlnNewsNewsWorld, "GetNewsWorld");

            return group;
        }

        public static RouteGroupBuilder MapNewsHealthEducationEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsHealthEducationPageResponse>(DrupalContentConstants.QlnNewsNewsHealthEducation, "GetNewsHealthEducation");

            return group;
        }

        public static RouteGroupBuilder MapNewsLawEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsLawPageResponse>(DrupalContentConstants.QlnNewsNewsLaw, "GetNewsLaw");

            return group;
        }

        public static RouteGroupBuilder MapNewsQatarEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsQatarPageResponse>(DrupalContentConstants.QlnNewsNewsQatar, "GetNewsQatar");

            return group;

        }

        public static RouteGroupBuilder MapCommunityMorePostsEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<CommunityMorePostsResponse>(DrupalContentConstants.QlnCommunityMorePosts, "GetCommunityMorePosts");

            return group;

        }

        public static RouteGroupBuilder MapContentQueueEndpoint(this RouteGroupBuilder group)
        {

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
                    var model = await svc.GetPostsFromDrupalAsync<dynamic>(queue, cancellationToken);

                    //if (model != null)
                    //{
                    //    var genericResponse = new GenericNewsPageResponse
                    //    {
                    //        Results = new GenericNewsResults
                    //        {
                    //            TopStory = model.Results.TopStory,
                    //        }
                    //    };

                    //    return Results.Ok(genericResponse);

                    //}

                    //return Results.NotFound();
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
            .WithName("GetLandingByQueue")
            .WithDescription("Tester method for testing out as yet unmapped Drupal queues - returns a dynamic object so isnt very efficient")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapContentEventsEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/events
            group.MapGet("/events", async (
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var model = await svc.GetEventsFromDrupalAsync(cancellationToken);
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

        public static RouteGroupBuilder MapContentCommunityEndpoint(this RouteGroupBuilder group)
        {
            // GET /api/content/categories
            group.MapGet("/community", async (
                    HttpContext context,
                    [FromQuery] string forum_id,
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
                        forum_id,
                        cancellationToken,
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
                        title: "Content Categories Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetContentCommunities")
            .WithTags("Content");

            return group;
        }
        public static RouteGroupBuilder MapContentCategoriesEndpoint(this RouteGroupBuilder group)
        {
            const int CATEGORY_CACHE_EXPIRY_IN_MINS = 60;

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
                    context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromMinutes(CATEGORY_CACHE_EXPIRY_IN_MINS)
                    };

                    // Add Vary header for the User-Agent
                    context.Response.Headers[HeaderNames.Vary] = "User-Agent";

                    var model = await svc.GetCategoriesFromDrupalAsync(cancellationToken);

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

        public static RouteGroupBuilder MapGetNewsBySlugEndpoint(this RouteGroupBuilder group)
        {


            // GET /api/content/news/{slug}
            group.MapGet("/news/{*slug}", async (
                    [FromRoute] string slug,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var ad = await svc.GetNewsBySlugAsync(slug, cancellationToken);
                    return ad is null ? Results.NotFound() : Results.Ok(ad);
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

        public static RouteGroupBuilder MapPostForumPostEndpoint(this RouteGroupBuilder group)
        {


            // GET /api/content/post/save
            group.MapPost("/post/save", async (
                    [FromBody] CreatePostRequest request,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var comment = await svc.CreatePostOnDrupalAsync(request, cancellationToken);
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
        public static RouteGroupBuilder MapGetEventBySlugEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/event/{slug}
            group.MapGet("/event/{*slug}", async (
                    [FromRoute] string slug,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var ad = await svc.GetEventBySlugAsync(slug, cancellationToken);
                    return ad is null ? Results.NotFound() : Results.Ok(ad);
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

        public static RouteGroupBuilder MapGetPostBySlugEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/event/{slug}
            group.MapGet("/post/{*slug}", async (
                    [FromRoute] string slug,
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var ad = await svc.GetPostBySlugAsync(slug, cancellationToken);
                    return ad is null ? Results.NotFound() : Results.Ok(ad);
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

        private static RouteGroupBuilder GenerateLandingEndpoint<T>(this RouteGroupBuilder group, string QueueName, string Name)
        {

            // GET /api/content/landing
            group.MapGet($"{QueueName}/landing", async (
                    [FromServices] IContentService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var model = await svc.GetPostsFromDrupalAsync<T>(QueueName, cancellationToken);

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
    }
}

