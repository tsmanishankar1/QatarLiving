using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Common.Infrastructure.CustomEndpoints.ContentEndpoints
{
    public static class ContentEndpoints
    {
        /// <summary>
        /// Maps Content endpoints: detail and landing.
        /// </summary>
        public static RouteGroupBuilder MapContentLandingEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.MapGet("/landing", async (
                    [FromServices] IContentService svc)
                =>
            {
                try
                {
                    var model = await svc.GetLandingPageAsync();
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
            .WithName("GetContentLanding")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapGetPostBySlugEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/post/{slug}
            group.MapGet("/post/{slug}", async (
                    [FromRoute] string slug,
                    [FromServices] IContentService svc)
                =>
            {
                try
                {
                    var ad = await svc.GetPostBySlugAsync(slug);
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
                    [FromServices] IContentService svc)
                =>
            {
                try
                {
                    var ad = await svc.GetEventBySlugAsync(slug);
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

    }
}

