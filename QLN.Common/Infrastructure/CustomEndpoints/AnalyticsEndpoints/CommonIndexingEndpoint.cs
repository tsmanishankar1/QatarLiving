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
using System.Runtime.Intrinsics.Arm;

namespace QLN.Common.Infrastructure.CustomEndpoints;

public static class CommonIndexingEndpoints
{
    public static RouteGroupBuilder MapCommonIndexingEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/search", async (
                [FromQuery] string index,
                [FromBody] CommonSearchRequest req,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Index parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/search"
                });

            if (req is null)
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Request payload is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/search"
                });

            try
            {
                var response = await svc.SearchAsync(index, req);
                return Results.Ok(response);
            }
            catch (ArgumentNullException ex)
            {
                logger.LogWarning(ex, "Null argument provided for search on index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/search?index={index}"
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Invalid filter") ||
                                              ex.Message.Contains("Invalid date") ||
                                              ex.Message.Contains("Empty collection") ||
                                              ex.Message.Contains("Unsupported filter") ||
                                              ex.Message.Contains("Error building filter") ||
                                              ex.Message.Contains("Error processing filters"))
            {
                logger.LogWarning(ex, "Invalid filter provided for index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Filter",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/search?index={index}"
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("PageNumber") ||
                                              ex.Message.Contains("PageSize") ||
                                              ex.Message.Contains("OrderBy"))
            {
                logger.LogWarning(ex, "Invalid pagination or sorting parameter for index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Parameter",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/search?index={index}"
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid argument for search on index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Argument",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/search?index={index}"
                });
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Unsupported index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Unsupported Index",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/search?index={index}"
                });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search service error for index '{Index}'", index);
                var status = ex.Status >= 400 && ex.Status < 600 ? ex.Status : StatusCodes.Status502BadGateway;
                return Results.Problem(
                    title: "Search Service Error",
                    detail: $"Azure Search service encountered an error: {ex.Message}",
                    statusCode: status,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                    instance: $"/api/indexes/search?index={index}"
                );
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Search operation failed for index '{Index}'", index);
                return Results.Problem(
                    title: "Search Operation Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/search?index={index}"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during search for index '{Index}'", index);
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while processing your search request.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/search?index={index}"
                );
            }
        })
        .WithName("GlobalSearch")
        .WithTags("Indexes")
        .WithSummary("Search documents in any index")
        .WithDescription("Search for documents using ?index=classifiedsitems. Supports both regular property filters, JSON attribute filters, and date filtering.");

        group.MapPost("/raw", async (
                [FromQuery] string index,
                [FromBody] RawSearchRequest req,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac,
                HttpContext http
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing.Raw");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "index is required",
                    Status = 400,
                    Instance = "/api/indexes/raw"
                });

            if (req is null || string.IsNullOrWhiteSpace(req.Filter))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Request payload with a non-empty 'filter' is required",
                    Status = 400,
                    Instance = "/api/indexes/raw"
                });

            try
            {
                object results = index.ToLowerInvariant() switch
                {
                    // classifieds & Services
                    ConstantValues.IndexNames.ClassifiedsItemsIndex =>
                        await svc.SearchRawAsync<ClassifiedsItemsIndex>(index, req, http.RequestAborted),
                    ConstantValues.IndexNames.ClassifiedsPrelovedIndex =>
                        await svc.SearchRawAsync<ClassifiedsPrelovedIndex>(index, req, http.RequestAborted),
                    ConstantValues.IndexNames.ClassifiedsCollectiblesIndex =>
                        await svc.SearchRawAsync<ClassifiedsCollectiblesIndex>(index, req, http.RequestAborted),
                    ConstantValues.IndexNames.ClassifiedsDealsIndex =>
                        await svc.SearchRawAsync<ClassifiedsDealsIndex>(index, req, http.RequestAborted),
                    ConstantValues.IndexNames.ServicesIndex =>
                        await svc.SearchRawAsync<ServicesIndex>(index, req, http.RequestAborted),
                    ConstantValues.IndexNames.ClassifiedStoresIndex =>
                        await svc.SearchRawAsync<ClassifiedStoresIndex>(index, req, http.RequestAborted),

                    // content
                    ConstantValues.IndexNames.ContentNewsIndex =>
                        await svc.SearchRawAsync<ContentNewsIndex>(index, req, http.RequestAborted),
                    ConstantValues.IndexNames.ContentEventsIndex =>
                        await svc.SearchRawAsync<ContentEventsIndex>(index, req, http.RequestAborted),
                    ConstantValues.IndexNames.ContentCommunityIndex =>
                        await svc.SearchRawAsync<ContentCommunityIndex>(index, req, http.RequestAborted),

                    _ => throw new NotSupportedException($"Unknown index '{index}'")
                };

                return Results.Ok(results);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid RAW search args for index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Argument",
                    Detail = ex.Message,
                    Status = 400,
                    Instance = $"/api/indexes/raw?index={index}"
                });
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Unsupported index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Unsupported Index",
                    Detail = ex.Message,
                    Status = 400,
                    Instance = $"/api/indexes/raw?index={index}"
                });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search RAW error for index '{Index}'", index);
                var status = ex.Status >= 400 && ex.Status < 600 ? ex.Status : StatusCodes.Status502BadGateway;
                return Results.Problem(
                    title: "Search Service Error",
                    detail: $"Azure Search service encountered an error: {ex.Message}",
                    statusCode: status,
                    instance: $"/api/indexes/raw?index={index}"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected RAW search error for index '{Index}'", index);
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while processing your raw search request.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    instance: $"/api/indexes/raw?index={index}"
                );
            }
        })
        .WithName("RawSearch")
        .WithTags("Indexes")
        .WithSummary("Raw Azure Search (POST)")
        .WithDescription("Executes a raw search using an OData filter, orderBy, top/skip and text.");

        group.MapPost("/getAll", async (
            [FromQuery] string index,
            [FromBody] CommonSearchRequest req,
            [FromServices] ISearchService svc,
            [FromServices] ILoggerFactory logFac
        ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Index parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/getAll"
                });

            if (req is null)
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Request payload is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/getAll"
                });

            try
            {
                var response = await svc.GetAllAsync(index, req);
                return Results.Ok(response);
            }
            catch (ArgumentNullException ex)
            {
                logger.LogWarning(ex, "Null argument provided for GetAll on index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/getAll?index={index}"
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Invalid filter") ||
                                              ex.Message.Contains("Invalid date") ||
                                              ex.Message.Contains("Empty collection") ||
                                              ex.Message.Contains("Unsupported filter") ||
                                              ex.Message.Contains("Error building filter") ||
                                              ex.Message.Contains("Error processing filters"))
            {
                logger.LogWarning(ex, "Invalid filter provided for GetAll on index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Filter",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/getAll?index={index}"
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("PageNumber") ||
                                              ex.Message.Contains("PageSize") ||
                                              ex.Message.Contains("OrderBy"))
            {
                logger.LogWarning(ex, "Invalid pagination or sorting parameter for GetAll on index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Parameter",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/getAll?index={index}"
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid argument for GetAll on index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Argument",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/getAll?index={index}"
                });
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Unsupported index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Unsupported Index",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/getAll?index={index}"
                });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search service error for GetAll on index '{Index}'", index);
                var status = ex.Status >= 400 && ex.Status < 600 ? ex.Status : StatusCodes.Status502BadGateway;
                return Results.Problem(
                    title: "Search Service Error",
                    detail: $"Azure Search service encountered an error: {ex.Message}",
                    statusCode: status,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                    instance: $"/api/indexes/getAll?index={index}"
                );
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "GetAll operation failed for index '{Index}'", index);
                return Results.Problem(
                    title: "GetAll Operation Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/getAll?index={index}"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during GetAll for index '{Index}'", index);
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while processing your GetAll request.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/getAll?index={index}"
                );
            }
        })
        .WithName("GetAllIndexDocuments")
        .WithTags("Indexes")
        .WithSummary("Get all documents with filters, no default sorting")
        .WithDescription("Returns all documents using filters, but skips internal default sorting logic. Supports date filtering and JSON attribute filters.");

        group.MapPost("/upload", async (
                [FromBody] CommonIndexRequest req,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (req is null)
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Request payload is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/upload"
                });

            try
            {
                var msg = await svc.UploadAsync(req);
                return Results.Ok(new { message = msg, success = true });
            }
            catch (ArgumentNullException ex)
            {
                logger.LogWarning(ex, "Null argument provided for upload");
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/upload"
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid upload request for index '{IndexName}'", req?.IndexName);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Upload Request",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/upload"
                });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search upload error for index '{IndexName}'", req?.IndexName);
                var status = ex.Status >= 400 && ex.Status < 600 ? ex.Status : StatusCodes.Status502BadGateway;
                return Results.Problem(
                    title: "Upload Service Error",
                    detail: $"Azure Search service encountered an error during upload: {ex.Message}",
                    statusCode: status,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                    instance: "/api/indexes/upload"
                );
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Upload operation failed for index '{IndexName}'", req?.IndexName);
                return Results.Problem(
                    title: "Upload Operation Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: "/api/indexes/upload"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected upload error for index '{IndexName}'", req?.IndexName);
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while processing your upload request.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: "/api/indexes/upload"
                );
            }
        })
        .WithName("UploadDocument")
        .WithTags("Indexes")
        .WithSummary("Upload a document to search index")
        .WithDescription("Upload/upsert a document. Specify indexName in payload.");

        group.MapPut("/update", async (
                [FromBody] CommonIndexRequest req,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (req is null)
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Request payload is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/update"
                });

            try
            {
                var msg = await svc.UploadAsync(req);
                return Results.Ok(new { message = msg, success = true });
            }
            catch (ArgumentNullException ex)
            {
                logger.LogWarning(ex, "Null argument provided for update");
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/update"
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid update request for index '{IndexName}'", req?.IndexName);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Update Request",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/update"
                });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search update error for index '{IndexName}'", req?.IndexName);
                var status = ex.Status >= 400 && ex.Status < 600 ? ex.Status : StatusCodes.Status502BadGateway;
                return Results.Problem(
                    title: "Update Service Error",
                    detail: $"Azure Search service encountered an error during update: {ex.Message}",
                    statusCode: status,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                    instance: "/api/indexes/update"
                );
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Update operation failed for index '{IndexName}'", req?.IndexName);
                return Results.Problem(
                    title: "Update Operation Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: "/api/indexes/update"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected update error for index '{IndexName}'", req?.IndexName);
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while processing your update request.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: "/api/indexes/update"
                );
            }
        })
        .WithName("UpdateDocument")
        .WithTags("Indexes")
        .WithSummary("Update a document in search index")
        .WithDescription("Update/upsert a document. Same as upload operation.");

        group.MapGet("/{index}/{id}", async (
                [FromRoute] string index,
                [FromRoute] string id,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Index parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/{index}/{id}"
                });

            if (string.IsNullOrWhiteSpace(id))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "ID parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/{index}/{id}"
                });

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
                    ConstantValues.IndexNames.ClassifiedStoresIndex =>
                        await svc.GetByIdAsync<ClassifiedStoresIndex>(index, id),
                    ConstantValues.IndexNames.ContentNewsIndex =>
                        await svc.GetByIdAsync<ContentNewsIndex>(index, id),
                    ConstantValues.IndexNames.ContentEventsIndex =>
                        await svc.GetByIdAsync<ContentEventsIndex>(index, id),
                    ConstantValues.IndexNames.ContentCommunityIndex =>
                        await svc.GetByIdAsync<ContentCommunityIndex>(index, id),
                    _ => throw new NotSupportedException($"Unknown index '{index}'")
                };

                return result is null
                    ? Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = $"Document with ID '{id}' not found in index '{index}' or is inactive",
                        Status = 404,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                        Instance = $"/api/indexes/{index}/{id}"
                    })
                    : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid argument for GetById on index '{Index}', id '{Id}'", index, id);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Argument",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/{index}/{id}"
                });
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Unsupported index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Unsupported Index",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/{index}/{id}"
                });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search GetById error for index '{Index}', id '{Id}'", index, id);
                var status = ex.Status >= 400 && ex.Status < 600 ? ex.Status : StatusCodes.Status502BadGateway;
                return Results.Problem(
                    title: "GetById Service Error",
                    detail: $"Azure Search service encountered an error: {ex.Message}",
                    statusCode: status,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                    instance: $"/api/indexes/{index}/{id}"
                );
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "GetById operation failed for index '{Index}', id '{Id}'", index, id);
                return Results.Problem(
                    title: "GetById Operation Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/{index}/{id}"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected lookup error for index '{Index}', id '{Id}'", index, id);
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while retrieving the document.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/{index}/{id}"
                );
            }
        })
        .WithName("GetDocumentById")
        .WithTags("Indexes")
        .WithSummary("Get a document by index and ID")
        .WithDescription("Examples: /api/indexes/classifiedsitems/test_001");

        group.MapDelete("/{index}/{id}", async (
               [FromRoute] string index,
               [FromRoute] string id,
               [FromServices] ISearchService svc,
               [FromServices] ILoggerFactory logFac
           ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Index parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/{index}/{id}"
                });

            if (string.IsNullOrWhiteSpace(id))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "ID parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/{index}/{id}"
                });

            try
            {
                await svc.DeleteAsync(index, id);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid argument for delete on index '{Index}', id '{Id}'", index, id);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Argument",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/{index}/{id}"
                });
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Document not found for delete on index '{Index}', id '{Id}'", index, id);
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = 404,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Instance = $"/api/indexes/{index}/{id}"
                });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Delete operation failed for index '{Index}', id '{Id}'", index, id);
                return Results.Problem(
                    title: "Delete Operation Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/{index}/{id}"
                );
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search delete error for index '{Index}', id '{Id}'", index, id);
                var status = ex.Status >= 400 && ex.Status < 600 ? ex.Status : StatusCodes.Status502BadGateway;
                return Results.Problem(
                    title: "Delete Service Error",
                    detail: $"Azure Search service encountered an error during delete: {ex.Message}",
                    statusCode: status,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                    instance: $"/api/indexes/{index}/{id}"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected delete error for index '{Index}', id '{Id}'", index, id);
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while deleting the document.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/{index}/{id}"
                );
            }
        })
       .WithName("DeleteDocumentById")
       .WithTags("Indexes")
       .WithSummary("Delete a document by index and ID")
       .WithDescription("Examples: DELETE /api/indexes/classifiedsitems/test_001");

        group.MapGet("/{index}/{slug}/details", async (
            [FromServices] ISearchService svc,
            [FromServices] ILoggerFactory logFac,
            [FromRoute] string index,
            [FromRoute] string slug,
            [FromQuery] int similarPageSize = 10
        ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Index parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/{index}/{id}/details"
                });

            if (string.IsNullOrWhiteSpace(slug))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Slug parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/{index}/{id}/details"
                });

            if (similarPageSize <= 0 || similarPageSize > 100)
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "SimilarPageSize must be between 1 and 100",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/{index}/{slug}/details"
                });

            try
            {
                object result = index.ToLowerInvariant() switch
                {
                    ConstantValues.IndexNames.ClassifiedsItemsIndex =>
                        await svc.GetBySlugWithSimilarAsync<ClassifiedsItemsIndex>(index, slug, similarPageSize),
                    ConstantValues.IndexNames.ClassifiedsPrelovedIndex =>
                        await svc.GetBySlugWithSimilarAsync<ClassifiedsPrelovedIndex>(index, slug, similarPageSize),
                    ConstantValues.IndexNames.ClassifiedsCollectiblesIndex =>
                        await svc.GetBySlugWithSimilarAsync<ClassifiedsCollectiblesIndex>(index, slug, similarPageSize),
                    ConstantValues.IndexNames.ClassifiedsDealsIndex =>
                        await svc.GetBySlugWithSimilarAsync<ClassifiedsDealsIndex>(index, slug, similarPageSize),
                    ConstantValues.IndexNames.ServicesIndex =>
                        await svc.GetBySlugWithSimilarAsync<ServicesIndex>(index, slug, similarPageSize),
                    _ => throw new NotSupportedException($"Details not supported for index '{index}'")
                };
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Document not found for details on index '{Index}', id '{Id}'", index, slug);
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Instance = $"/api/indexes/{index}/{slug}/details"
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid argument for details on index '{Index}', id '{Id}'", index, slug);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Argument",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/{index}/{slug}/details"
                });
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Unsupported operation for index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Not Supported",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/{index}/{slug}/details"
                });
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "Azure Search details error for index '{Index}', id '{Id}'", index, slug);
                var status = ex.Status >= 400 && ex.Status < 600 ? ex.Status : StatusCodes.Status502BadGateway;
                return Results.Problem(
                    title: "Details Service Error",
                    detail: $"Azure Search service encountered an error: {ex.Message}",
                    statusCode: status,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.3",
                    instance: $"/api/indexes/{index}/{slug}/details"
                );
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Details operation failed for index '{Index}', id '{Id}'", index, slug);
                return Results.Problem(
                    title: "Details Operation Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/{index}/{slug}/details"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected details error for index '{Index}', id '{Id}'", index, slug);
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while retrieving document details.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/{index}/{slug}/details"
                );
            }
        })
        .WithName("GetDocumentWithSimilar")
        .WithTags("Indexes")
        .WithSummary("Get a document plus similar items")
        .WithDescription("Examples: /api/indexes/classifiedsitems/test_001/details?similarPageSize=10");

        group.MapGet("/{index}/health", async (
                [FromRoute] string index,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Index parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/{index}/health"
                });

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
        .WithTags("Indexes")
        .WithSummary("Check health of a specific index")
        .WithDescription("Examples: /api/indexes/classifiedsitems/health");

        group.MapGet("/{index}/stats", async (
                [FromRoute] string index,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Index parameter is required",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = "/api/indexes/{index}/stats"
                });

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
            catch (ArgumentException ex) when (ex.Message.Contains("Invalid filter") ||
                                              ex.Message.Contains("Invalid date") ||
                                              ex.Message.Contains("Empty collection") ||
                                              ex.Message.Contains("Unsupported filter") ||
                                              ex.Message.Contains("Error building filter") ||
                                              ex.Message.Contains("Error processing filters"))
            {
                logger.LogError(ex, "Invalid filter in stats request for index '{Index}'", index);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Filter",
                    Detail = ex.Message,
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = $"/api/indexes/{index}/stats"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stats failed for index '{Index}'", index);
                return Results.Problem(
                    title: "Stats Error",
                    detail: "An error occurred while retrieving index statistics.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    instance: $"/api/indexes/{index}/stats"
                );
            }
        })
        .WithName("IndexStats")
        .WithTags("Indexes")
        .WithSummary("Get index statistics")
        .WithDescription("Examples: /api/indexes/classifiedsitems/stats");

        group.MapGet("/{index}/suggestions", async (
                [FromRoute] string index,
                [FromQuery] string q,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac,
                [FromQuery] int size = 10
            ) =>
        {
            var logger = logFac.CreateLogger("CommonIndexing");

            if (string.IsNullOrWhiteSpace(index))
                return Results.BadRequest("Index parameter is required");

            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Results.Ok(new List<string>());

            if (size <= 0 || size > 50)
                size = 10;

            try
            {
                var suggestions = await svc.GetSearchSuggestionsAsync(index, q, size);
                return Results.Ok(suggestions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Suggestions failed for index '{Index}', query '{Query}'", index, q);
                return Results.Ok(new List<string>());
            }
        })
        .WithName("GetSearchSuggestions")
        .WithTags("Indexes")
        .WithSummary("Get search suggestions for autocomplete")
        .WithDescription("Examples: /api/indexes/classifiedsitems/suggestions?q=micr&size=10");

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

            ?? "unknown";
    }
}