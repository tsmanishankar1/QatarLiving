using System;
using System.Linq;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.SearchService.IndexModels;
using System.Data;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.DTOs;
using QLN.Common.DTO_s;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace QLN.SearchService.CustomEndpoints
{
    public static class CommonIndexingEndpoints
    {
        public static RouteGroupBuilder MapCommonIndexingEndpoints(this RouteGroupBuilder group)
        {
            // SEARCH
            group.MapPost("/search", async (
                    [FromRoute] string vertical,
                    [FromBody] SearchRequest req,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("CommonIndexing");
                if (req is null)
                    return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Payload required", Status = 400 });

                try
                {
                    var response = await svc.SearchAsync(vertical, req);
                    return Results.Ok(response);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid vertical '{Vertical}'", vertical);
                    return Results.BadRequest(new ProblemDetails { Title = "Invalid Vertical", Detail = ex.Message, Status = 400 });
                }
                catch (RequestFailedException ex)
                {
                    logger.LogError(ex, "Azure Search error on '{Vertical}'", vertical);
                    var status = ex.Status >= 400 && ex.Status < 600
                        ? ex.Status
                        : StatusCodes.Status502BadGateway;
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: status,
                        instance: $"/api/{vertical}/search"
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Search error on '{Vertical}'", vertical);
                    return Results.Problem("Search Error", ex.Message, 500);
                }
            })
            .WithName("CommonSearch")
            .WithTags("Indexing");

            // UPLOAD
            group.MapPost("/upload", async (
                    [FromBody] CommonIndexRequest req,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("CommonIndexing");
                try
                {
                    var msg = await svc.UploadAsync(req);
                    return Results.Ok(msg);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid upload for '{Vertical}'", req.VerticalName);
                    return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = ex.Message, Status = 400 });
                }
                catch (RequestFailedException ex)
                {
                    logger.LogError(ex, "Azure Search error on upload for '{Vertical}'", req.VerticalName);
                    return Results.Problem("Azure Search Error", ex.Message, 502);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected upload error for '{Vertical}'", req.VerticalName);
                    return Results.Problem("Indexing Error", ex.Message, 500);
                }
            })
            .WithName("CommonUpload")
            .WithTags("Indexing");

            // GET BY ID
            group.MapGet("/{id}", async (
                    [FromRoute] string vertical,
                    [FromRoute] string id,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("CommonIndexing");
                try
                {
                    object? result = vertical.ToLowerInvariant() switch
                    {
                        "classifieds" => await svc.GetByIdAsync<ClassifiedsIndex>(vertical, id),
                        "backofficemaster" => await svc.GetByIdAsync<BackofficemasterIndex>(vertical, id),
                        // add other verticals here...
                        _ => throw new NotSupportedException($"Unknown vertical '{vertical}'")
                    };
                    return result is null
                        ? Results.NotFound(new ProblemDetails { Title = "Not Found", Detail = $"No '{id}' in '{vertical}'", Status = 404 })
                        : Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid vertical '{Vertical}'", vertical);
                    return Results.BadRequest(new ProblemDetails { Title = "Invalid Vertical", Detail = ex.Message, Status = 400 });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lookup error for '{Vertical}/{Id}'", vertical, id);
                    return Results.Problem("Lookup Error", ex.Message, 500);
                }
            })
            .WithName("CommonGetById")
            .WithTags("Indexing");

            // UPDATE (same as upload for upsert)
            group.MapPut("/update", async (
                    [FromBody] CommonIndexRequest req,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("CommonIndexing");
                try
                {
                    var msg = await svc.UploadAsync(req);
                    return Results.Ok(msg);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid update for '{Vertical}'", req.VerticalName);
                    return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = ex.Message, Status = 400 });
                }
                catch (RequestFailedException ex)
                {
                    logger.LogError(ex, "Azure Search error on update '{Vertical}'", req.VerticalName);
                    return Results.Problem("Azure Search Error", ex.Message, 502);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected update error for '{Vertical}'", req.VerticalName);
                    return Results.Problem("Update Error", ex.Message, 500);
                }
            })
            .WithName("CommonUpdate")
            .WithTags("Indexing");

            return group;
        }
    }
}
