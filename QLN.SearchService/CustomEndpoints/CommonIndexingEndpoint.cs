// QLN.SearchService.CustomEndpoints/CommonIndexingEndpoints.cs
using System;
using System.Linq;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.SearchService.IService;
using QLN.SearchService.IndexModels;
using QLN.SearchService.Models;

namespace QLN.SearchService.CustomEndpoints
{
    public static class CommonIndexingEndpoints
    {
        public static RouteGroupBuilder MapCommonIndexingEndpoints(this RouteGroupBuilder group)
        {
            // ─────────── SEARCH ───────────
            group.MapPost("/search", async (
                    [FromRoute] string vertical,
                    [FromBody] SearchRequest req,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("CommonIndexing");

                if (req is null)
                {
                    logger.LogWarning("Search called with null payload on '{Vertical}'", vertical);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Search payload is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{vertical}/search"
                    });
                }

                try
                {
                    var items = await svc.Search(vertical, req);
                    var response = new CommonResponse
                    {
                        VerticalName = vertical,
                        ClassifiedsItems = items.ToList()
                    };
                    return Results.Ok(response);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid vertical '{Vertical}' in search", vertical);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Vertical",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{vertical}/search"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Search error on '{Vertical}'", vertical);
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{vertical}/search"
                    );
                }
            })
            .WithName("CommonSearch")
            .WithTags("Indexing")
            .WithSummary("Full-text search returning CommonResponse")
            .Produces<CommonResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // ─────────── UPLOAD ───────────
            group.MapPost("/upload", async (
                    [FromBody] CommonIndexRequest request,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("CommonIndexing");

                if (string.IsNullOrWhiteSpace(request.VerticalName)
                    || (request.VerticalName.Equals(Constants.Constants.classifieds, StringComparison.OrdinalIgnoreCase)
                        && request.ClassifiedsItem is null))
                {
                    logger.LogWarning("Upload called with invalid request: {@Request}", request);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "VerticalName and corresponding item must be provided.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{request.VerticalName}/upload"
                    });
                }

                try
                {
                    var msg = await svc.Upload(request);
                    return Results.Ok(msg);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid upload request for vertical '{Vertical}'", request.VerticalName);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Vertical or Payload",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{request.VerticalName}/upload"
                    });
                }
                catch (RequestFailedException ex)
                {
                    logger.LogError(ex, "Azure Search error on upload for '{Vertical}'", request.VerticalName);
                    return Results.Problem(
                        title: "Azure Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway,
                        instance: $"/api/{request.VerticalName}/upload"
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected upload error for '{Vertical}'", request.VerticalName);
                    return Results.Problem(
                        title: "Indexing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{request.VerticalName}/upload"
                    );
                }
            })
            .WithName("CommonUpload")
            .WithTags("Indexing")
            .WithSummary("Upload a single item via CommonIndexRequest")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // ─────────── GET BY ID ───────────
            group.MapGet("/{id}", async (
                    [FromRoute] string vertical,
                    [FromRoute] string id,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("CommonIndexing");

                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogWarning("GetById called with empty id on '{Vertical}'", vertical);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{vertical}/{id}"
                    });
                }

                try
                {
                    var doc = await svc.GetById(vertical, id);
                    return doc is null
                        ? Results.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No document '{id}' in '{vertical}'.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = $"/api/{vertical}/{id}"
                        })
                        : Results.Ok(doc);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid vertical '{Vertical}' in GetById", vertical);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Vertical",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{vertical}/{id}"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GetById error on '{Vertical}/{Id}'", vertical, id);
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{vertical}/{id}"
                    );
                }
            })
            .WithName("CommonGetById")
            .WithTags("Indexing")
            .WithSummary("Get document by key")
            .Produces<ClassifiedIndex>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // ─────────── UPDATE ───────────
            group.MapPut("/update", async (
                    [FromBody] CommonIndexRequest request,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("CommonIndexing");
                var vertical = request.VerticalName?.ToLowerInvariant() ?? "";

                // Validate
                if (vertical != Constants.Constants.classifieds || request.ClassifiedsItem is null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "You must supply verticalName:'classifieds' and a ClassifiedsItem to update.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{vertical}/update"
                    });
                }

                try
                {
                    var msg = await svc.Upload(request);
                    return Results.Ok(msg);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid update request for '{Vertical}'", vertical);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Payload",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{vertical}/update"
                    });
                }
                catch (RequestFailedException ex)
                {
                    logger.LogError(ex, "Azure Search error updating '{Vertical}'", vertical);
                    return Results.Problem(
                        title: "Azure Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway,
                        instance: $"/api/{vertical}/update"
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error on update for '{Vertical}'", vertical);
                    return Results.Problem(
                        title: "Update Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{vertical}/update"
                    );
                }
            })
            .WithName("CommonUpdate")
            .WithTags("Indexing")
            .WithSummary("Update an existing document")
            .Produces<string>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(502)
            .ProducesProblem(500);

            return group;
        }
    }
}
