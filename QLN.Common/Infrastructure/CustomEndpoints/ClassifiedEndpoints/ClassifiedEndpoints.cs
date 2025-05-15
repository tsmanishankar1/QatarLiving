using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class ClassifiedEndpoints
    {
        /// <summary>
        /// Maps Classified endpoints: search, detail, upload, and landing.
        /// </summary>
        public static RouteGroupBuilder MapClassifiedEndpoints(this RouteGroupBuilder group)
        {
            // POST /api/{vertical}/search
            group.MapPost("/search", async (
                    [FromRoute] string vertical,
                    [FromBody] ClassifiedSearchRequest req,
                    [FromServices] IClassifiedService svc)
                =>
            {
                if (string.IsNullOrWhiteSpace(vertical))
                    return Results.BadRequest(new { Message = "Vertical is required in route." });

                try
                {
                    var results = await svc.SearchAsync(vertical, req);
                    return Results.Ok(results);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("SearchClassified")
            .WithTags("Classified");

            // GET /api/{vertical}/{id}
            group.MapGet("/{id}", async (
                    [FromRoute] string vertical,
                    [FromRoute] string id,
                    [FromServices] IClassifiedService svc)
                =>
            {
                if (string.IsNullOrWhiteSpace(vertical) || string.IsNullOrWhiteSpace(id))
                    return Results.BadRequest(new { Message = "Vertical and Id are required." });

                try
                {
                    var ad = await svc.GetByIdAsync(vertical, id);
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
            .WithName("GetClassifiedById")
            .WithTags("Classified");

            // POST /api/{vertical}/upload
            group.MapPost("/upload", async (
                    [FromRoute] string vertical,
                    [FromBody] ClassifiedIndexDto doc,
                    [FromServices] IClassifiedService svc)
                =>
            {
                if (string.IsNullOrWhiteSpace(vertical) || doc == null)
                    return Results.BadRequest(new { Message = "Vertical and document payload are required." });

                try
                {
                    var msg = await svc.UploadAsync(vertical, doc);
                    return Results.Ok(new { Message = msg });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Upload Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("UploadClassified")
            .WithTags("Classified");

            // GET /api/{vertical}/landing
            group.MapGet("/landing", async (
                    [FromRoute] string vertical,
                    [FromServices] IClassifiedService svc)
                =>
            {
                if (string.IsNullOrWhiteSpace(vertical))
                    return Results.BadRequest(new { Message = "Vertical is required in route." });

                try
                {
                    var model = await svc.GetLandingPageAsync(vertical);
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
            .WithName("GetClassifiedLanding")
            .WithTags("Classified");

            return group;
        }
    }
}

