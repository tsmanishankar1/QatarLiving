using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;
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
            group.GenerateLandingEndpoint<QlnContentsDailyPageResponse>(ContentConstants.QlnContentsDaily, "GetContentsDaily"); 

            return group;
        }

        public static RouteGroupBuilder MapNewsCommunityEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsCommunityPageResponse>(ContentConstants.QlnNewsNewsCommunity, "GetNewsCommunity");

            return group;
        }

        public static RouteGroupBuilder MapNewsMiddleEastEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsMiddleEastPageResponse>(ContentConstants.QlnNewsNewsMiddleEast, "GetNewsMiddleEast");

            return group;
        }

        public static RouteGroupBuilder MapNewsWorldEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsWorldPageResponse>(ContentConstants.QlnNewsNewsWorld, "GetNewsWorld");

            return group;
        }

        public static RouteGroupBuilder MapNewsHealthEducationEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsHealthEducationPageResponse>(ContentConstants.QlnNewsNewsHealthEducation, "GetNewsHealthEducation");

            return group;
        }

        public static RouteGroupBuilder MapNewsLawEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsLawPageResponse>(ContentConstants.QlnNewsNewsLaw, "GetNewsLaw");

            return group;
        }

        public static RouteGroupBuilder MapNewsQatarEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.GenerateLandingEndpoint<NewsQatarPageResponse>(ContentConstants.QlnNewsNewsQatar, "GetNewsQatar");

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

        public static RouteGroupBuilder MapGetPostBySlugEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/post/{slug}
            group.MapGet("/post/{slug}", async (
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

        public static RouteGroupBuilder MapGetEventBySlugEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/event/{slug}
            group.MapGet("/event/{slug}", async (
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

