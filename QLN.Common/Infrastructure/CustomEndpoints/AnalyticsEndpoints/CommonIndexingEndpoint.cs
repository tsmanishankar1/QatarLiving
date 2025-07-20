using System;
using System.Linq;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Data;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.DTOs;
using QLN.Common.DTO_s;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Builder;
using QLN.Common.Infrastructure.Constants;
using Dapr;
using System.Net;

namespace QLN.Common.Infrastructure.CustomEndpoints;

public static class CommonIndexingEndpoints
{
    public static RouteGroupBuilder MapCommonIndexingEndpoints(this RouteGroupBuilder group)
    {
        // ✅ Global search endpoint - /api/indexes/search?index=classifiedsitems
        group.MapPost("/search", async (
                [FromQuery] string index,
                [FromBody] CommonSearchRequest req,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Index parameter required", Status = 400 });

            if (req is null)
                return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Payload required", Status = 400 });

            try
            {
                var response = await svc.SearchAsync(index, req);
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails { Title = "Invalid Index", Detail = ex.Message, Status = 400 });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search error on '{Index}'", index);
                var status = ex.Status >= 400 && ex.Status < 600
                    ? ex.Status
                    : StatusCodes.Status502BadGateway;
                return Results.Problem(
                    title: "Search Error",
                    detail: ex.Message,
                    statusCode: status,
                    instance: $"/api/indexes/search?index={index}"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Search error on '{Index}'", index);
                return Results.Problem("Search Error", ex.Message, 500);
            }
        })
        .WithName("GlobalSearch")
        .WithTags("Search")
        .WithSummary("Search documents in any index")
        .WithDescription("Search for documents using ?index=classifiedsitems. Supports both regular property filters and JSON attribute filters.");

        // ✅ Upload endpoint - /api/indexes/upload
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
                return Results.Ok(new { message = msg, success = true });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid upload for '{IndexName}'", req?.IndexName);
                return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = ex.Message, Status = 400 });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search error on upload for '{IndexName}'", req?.IndexName);
                return Results.Problem("Azure Search Error", ex.Message, 502);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected upload error for '{IndexName}'", req?.IndexName);
                return Results.Problem("Indexing Error", ex.Message, 500);
            }
        })
        .WithName("UploadDocument")
        .WithTags("Documents")
        .WithSummary("Upload a document to search index")
        .WithDescription("Upload/upsert a document. Specify indexName in payload.");

        // ✅ Update endpoint - /api/indexes/update
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
                return Results.Ok(new { message = msg, success = true });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid update for '{IndexName}'", req?.IndexName);
                return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = ex.Message, Status = 400 });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search error on update '{IndexName}'", req?.IndexName);
                return Results.Problem("Azure Search Error", ex.Message, 502);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected update error for '{IndexName}'", req?.IndexName);
                return Results.Problem("Update Error", ex.Message, 500);
            }
        })
        .WithName("UpdateDocument")
        .WithTags("Documents")
        .WithSummary("Update a document in search index")
        .WithDescription("Update/upsert a document. Same as upload operation.");

        // ✅ Get document by ID - /api/indexes/{index}/{id}
        group.MapGet("/{index}/{id}", async (
                [FromRoute] string index,
                [FromRoute] string id,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(id))
                return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Index and ID are required", Status = 400 });

            try
            {
                object? result = index.ToLowerInvariant() switch
                {
                    ConstantValues.IndexNames.ClassifiedsItemsIndex =>
                        await svc.GetByIdAsync<ClassifiedsItemsIndex>(index, id),
                    ConstantValues.IndexNames.ClassifiedsPrelovedIndex =>
                        await svc.GetByIdAsync<ClassifiedsPrelovedIndex>(index, id),
                    ConstantValues.IndexNames.ClassifiedsCollectiblesIndex =>
                        await svc.GetByIdAsync<ClassifiedsCollectiblesIndex>(index, id),
                    ConstantValues.IndexNames.ClassifiedsDealsIndex =>
                        await svc.GetByIdAsync<ClassifiedsDealsIndex>(index, id),
                    ConstantValues.IndexNames.ServicesIndex =>
                        await svc.GetByIdAsync<ServicesIndex>(index, id),
                    ConstantValues.IndexNames.LandingBackOfficeIndex =>
                        await svc.GetByIdAsync<LandingBackOfficeIndex>(index, id),
                    _ => throw new NotSupportedException($"Unknown index '{index}'")
                };

                return result is null
                    ? Results.NotFound(new ProblemDetails { Title = "Not Found", Detail = $"No '{id}' in '{index}'", Status = 404 })
                    : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails { Title = "Invalid Index", Detail = ex.Message, Status = 400 });
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Unsupported index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails { Title = "Unsupported Index", Detail = ex.Message, Status = 400 });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lookup error for '{Index}/{Id}'", index, id);
                return Results.Problem("Lookup Error", ex.Message, 500);
            }
        })
        .WithName("GetDocumentById")
        .WithTags("Documents")
        .WithSummary("Get a document by index and ID")
        .WithDescription("Examples: /api/indexes/classifiedsitems/test_001");

        // ✅ Delete document by ID - /api/indexes/{index}/{id}
        group.MapDelete("/{index}/{id}", async (
               [FromRoute] string index,
               [FromRoute] string id,
               [FromServices] ISearchService svc,
               [FromServices] ILoggerFactory logFac
           ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Index must be provided", Status = 400 });

            if (string.IsNullOrWhiteSpace(id))
                return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Id must be provided", Status = 400 });

            try
            {
                await svc.DeleteAsync(index, id);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid index or id: '{Index}', '{Id}'", index, id);
                return Results.BadRequest(new ProblemDetails { Title = "Invalid Argument", Detail = ex.Message, Status = 400 });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Delete error for '{Index}/{Id}'", index, id);
                return Results.Problem("Delete Error", ex.Message, 500);
            }
        })
       .WithName("DeleteDocumentById")
       .WithTags("Documents")
       .WithSummary("Delete a document by index and ID")
       .WithDescription("Examples: DELETE /api/indexes/classifiedsitems/test_001");

        // ✅ Get details with similar - /api/indexes/{index}/{id}/details
        group.MapGet("/{index}/{id}/details", async (
            [FromServices] ISearchService svc,
            [FromServices] ILoggerFactory logFac,
            [FromRoute] string index,
            [FromRoute] string id,
            [FromQuery] int similarPageSize = 10

        ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(id))
                return Results.BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Index and ID are required", Status = 400 });

            try
            {
                object result = index.ToLowerInvariant() switch
                {
                    ConstantValues.IndexNames.ClassifiedsItemsIndex =>
                        await svc.GetByIdWithSimilarAsync<ClassifiedsItemsIndex>(index, id, similarPageSize),
                    ConstantValues.IndexNames.ClassifiedsPrelovedIndex =>
                        await svc.GetByIdWithSimilarAsync<ClassifiedsPrelovedIndex>(index, id, similarPageSize),
                    ConstantValues.IndexNames.ClassifiedsCollectiblesIndex =>
                        await svc.GetByIdWithSimilarAsync<ClassifiedsCollectiblesIndex>(index, id, similarPageSize),
                    ConstantValues.IndexNames.ClassifiedsDealsIndex =>
                        await svc.GetByIdWithSimilarAsync<ClassifiedsDealsIndex>(index, id, similarPageSize),
                    ConstantValues.IndexNames.ServicesIndex =>
                        await svc.GetByIdWithSimilarAsync<ServicesIndex>(index, id, similarPageSize),
                    ConstantValues.IndexNames.LandingBackOfficeIndex =>
                        await svc.GetByIdWithSimilarAsync<LandingBackOfficeIndex>(index, id, similarPageSize),
                    _ => throw new NotSupportedException($"Details not supported for '{index}'")
                };
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = $"No '{id}' in '{index}'",
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Bad request {Index}/{Id}", index, id);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Argument",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, ex.Message);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Not Supported",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search failure on details for {Index}/{Id}", index, id);
                return Results.Problem(
                    title: "Search Error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status502BadGateway
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error on details for {Index}/{Id}", index, id);
                return Results.Problem("Lookup Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetDocumentWithSimilar")
        .WithTags("Documents")
        .WithSummary("Get a document plus similar items")
        .WithDescription("Examples: /api/indexes/classifiedsitems/test_001/details?similarPageSize=10");

        // ✅ Health check - /api/indexes/{index}/health
        group.MapGet("/{index}/health", async (
                [FromRoute] string index,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            try
            {
                var testSearch = new CommonSearchRequest
                {
                    Text = "*",
                    PageNumber = 1,
                    PageSize = 1
                };

                var result = await svc.SearchAsync(index, testSearch);

                return Results.Ok(new
                {
                    index = index,
                    status = "healthy",
                    totalDocuments = result.TotalCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health check failed for index '{Index}'", index);
                return Results.Ok(new
                {
                    index = index,
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        })
        .WithName("IndexHealthCheck")
        .WithTags("Management")
        .WithSummary("Check health of a specific index")
        .WithDescription("Examples: /api/indexes/classifiedsitems/health");

        // ✅ Index statistics - /api/indexes/{index}/stats
        group.MapGet("/{index}/stats", async (
                [FromRoute] string index,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            try
            {
                var searchRequest = new CommonSearchRequest
                {
                    Text = "*",
                    PageNumber = 1,
                    PageSize = 0
                };

                var result = await svc.SearchAsync(index, searchRequest);

                return Results.Ok(new
                {
                    index = index,
                    totalDocuments = result.TotalCount,
                    lastQueried = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stats failed for index '{Index}'", index);
                return Results.Problem("Stats Error", ex.Message, 500);
            }
        })
        .WithName("IndexStats")
        .WithTags("Management")
        .WithSummary("Get index statistics")
        .WithDescription("Examples: /api/indexes/classifiedsitems/stats");

        return group;
    }
}

public static class CommonIndexRequestExtensions
{
    public static string GetItemId(this CommonIndexRequest request)
    {
        return request.ClassifiedsItem?.Id
            ?? request.ClassifiedsPrelovedItem?.Id
            ?? request.ClassifiedsCollectiblesItem?.Id
            ?? request.ClassifiedsDealsItem?.Id
            ?? request.ServicesItem?.Id
            ?? request.MasterItem?.Id
            ?? "unknown";
    }
}