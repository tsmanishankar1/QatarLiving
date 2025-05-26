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
        public static RouteGroupBuilder MapGetContentByIdEndpoint(this RouteGroupBuilder group)
        {
            
            // GET /api/{vertical}/{id}
            group.MapGet("/content/{id}", async (
                    [FromRoute] string id,
                    [FromServices] IContentService svc)
                =>
            {
                try
                {
                    var ad = await svc.GetContentByIdAsync(id);
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
            .WithName("GetContentById")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapGetEventByIdEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/event/{id}
            group.MapGet("/event/{id}", async (
                    [FromRoute] string id,
                    [FromServices] IContentService svc)
                =>
            {
                try
                {
                    var ad = await svc.GetEventByIdAsync(id);
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
            .WithName("GetEventById")
            .WithTags("Content");

            return group;
        }

        public static RouteGroupBuilder MapContentLandingEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/content/landing
            group.MapGet("/content/landing", async (
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
    }
}

