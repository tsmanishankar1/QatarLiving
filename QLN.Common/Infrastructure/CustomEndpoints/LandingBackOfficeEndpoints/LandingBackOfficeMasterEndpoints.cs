using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IBackOfficeService;
using QLN.Common.Infrastructure.IService.ISearchService;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Common.Infrastructure.CustomEndpoints
{
    public static class LandingBackOfficeMasterEndpoints
    {
        public static RouteGroupBuilder MapBackOfficeMasterEndpoints(
            this RouteGroupBuilder group,
            string vertical,
            string routeSegment,
            string entityType)
        {

            group.MapPost($"/{routeSegment}", async Task<Results<
                    Ok<string>,
                    BadRequest<ProblemDetails>,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>
                > (
                    LandingBackOfficeRequestDto dto,
                    [FromServices] IBackOfficeService<LandingBackOfficeIndex> stateSvc,
                    [FromServices] ILoggerFactory loggerFactory,
                    [FromServices] DaprClient dapr,
                    CancellationToken ct
                ) =>
            {
                var logger = loggerFactory.CreateLogger("BackOfficeEndpoints");
                if (dto is null)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Request body must contain a LandingBackOfficeRequestDto.",
                        Status = StatusCodes.Status400BadRequest
                    });

                dto.PayloadJson ??= new CommonSearchRequest();

                if (!string.IsNullOrWhiteSpace(dto.EntityId))
                {
                    var filterKey = entityType switch
                    {
                        EntityTypes.FeaturedItems => "Id",
                        EntityTypes.Category => "CategoryId",
                        EntityTypes.FeaturedCategory => "CategoryId",
                        EntityTypes.FeaturedStores => "StoreId",
                        _ => "Id"
                    };

                    dto.PayloadJson.Filters[filterKey] = dto.EntityId;
                }

                string keyPart;
                if (!string.IsNullOrWhiteSpace(dto.EntityId))
                {
                    keyPart = dto.EntityId!;
                }
                else
                {
                    var concat = $"{dto.Title?.Trim()}|{dto.ParentId}|{dto.RediectUrl}";
                    var bytes = Encoding.UTF8.GetBytes(concat);
                    using var md5 = MD5.Create();
                    var hash = md5.ComputeHash(bytes);
                    keyPart = BitConverter.ToString(hash, 0, 4)
                                       .Replace("-", "")
                                       .ToLowerInvariant();
                }

                var id = $"{vertical}-{entityType}-{keyPart}";
                var doc = new LandingBackOfficeIndex
                {
                    Id = id,
                    Vertical = vertical,
                    EntityType = entityType,
                    Title = dto.Title!,
                    Description = dto.Description,
                    Order = dto.Order ?? 0,
                    ParentId = dto.ParentId,
                    IsActive = dto.IsActive,
                    RediectUrl = dto.RediectUrl,
                    ImageUrl = dto.ImageUrl,
                    ListingCount = dto.ListingCount,
                    RotationSeconds = dto.RotationSeconds,
                    EntityId = dto.EntityId
                };
                if (dto.PayloadJson != null)
                    doc.PayloadJson = JsonSerializer.Serialize(dto.PayloadJson);

                try
                {
                    logger.LogInformation("Upserting document: {Id}", doc.Id);
                    await stateSvc.UpsertState(doc, ct);

                    var msg = new IndexMessage
                    {
                        Vertical = vertical,
                        Action = "Upsert",
                        UpsertRequest = new CommonIndexRequest
                        {
                            IndexName = IndexNames.LandingBackOfficeIndex,
                            MasterItem = doc
                        }
                    };
                    await dapr.PublishEventAsync(PubSubName, PubSubTopics.IndexUpdates, msg, ct);
                    logger.LogInformation("Queued IndexMessage for {Id}", doc.Id);

                    return TypedResults.Ok($"Record saved and queued for indexing under id={doc.Id}");
                }
                catch (InvocationException ie)
                {
                    var httpEx = ie.InnerException as HttpRequestException;
                    var status = httpEx?.StatusCode ?? HttpStatusCode.InternalServerError;
                    var pd = new ProblemDetails
                    {
                        Title = status == HttpStatusCode.NotFound
                                 ? "Not Found"
                                 : "Dapr Invocation Error",
                        Detail = ie.Message,
                        Status = (int)status
                    };
                    return status switch
                    {
                        HttpStatusCode.BadRequest => TypedResults.BadRequest(pd),
                        HttpStatusCode.NotFound => TypedResults.NotFound(pd),
                        _ => TypedResults.Problem(pd.Title, pd.Detail, pd.Status)
                    };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to upsert or publish for {Id}", doc.Id);
                    var pd = new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    };
                    return TypedResults.Problem(pd.Title, pd.Detail, pd.Status);
                }
            });

            group.MapGet($"/{routeSegment}", async Task<Results<
                    Ok<List<object>>,
                    BadRequest<ProblemDetails>,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>
                > (
                    [FromServices] ISearchService searchSvc,
                    [FromServices] ILoggerFactory loggerFactory,
                    CancellationToken ct
                ) =>
            {
                var logger = loggerFactory.CreateLogger("BackOfficeEndpoints");
                var searchReq = new CommonSearchRequest
                {
                    Filters = new Dictionary<string, object> {
                        { "Vertical",   vertical },
                        { "EntityType", entityType }
                    }
                };

                try
                {
                    logger.LogInformation("Searching for {EntityType} under {Vertical}", entityType, vertical);
                    var resp = await searchSvc.SearchAsync(IndexNames.LandingBackOfficeIndex, searchReq);
                    var items = resp.MasterItems ?? new List<LandingBackOfficeIndex>();

                    static CommonSearchRequest? ParsePayload(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? null
                : JsonSerializer.Deserialize<CommonSearchRequest>(raw);

                    List<object> BuildHierarchyProjected(List<LandingBackOfficeIndex> src, string? parentId) =>
                        src
                            .Where(x => x.ParentId == parentId)
                            .OrderBy(x => x.Order)
                            .Select(x => new
                            {
                                x.Id,
                                x.EntityType,
                                x.Vertical,
                                x.Title,
                                x.Description,
                                x.Order,
                                x.ParentId,
                                x.IsActive,
                                x.RediectUrl,
                                x.ImageUrl,
                                x.ListingCount,
                                x.RotationSeconds,
                                x.EntityId,
                                PayloadJson = ParsePayload(x.PayloadJson),
                                Children = BuildHierarchyProjected(src, x.Id)
                            })
                            .ToList<object>();

                    if (items.Any(i => !string.IsNullOrWhiteSpace(i.ParentId)))
                        return TypedResults.Ok(BuildHierarchyProjected(items, null));

                    var flat = items
                        .OrderBy(i => i.Order)
                        .Select(i => new
                        {
                            i.Id,
                            i.EntityType,
                            i.Vertical,
                            i.Title,
                            i.Description,
                            i.Order,
                            i.ParentId,
                            i.IsActive,
                            i.RediectUrl,
                            i.ImageUrl,
                            i.ListingCount,
                            i.RotationSeconds,
                            i.EntityId,
                            PayloadJson = ParsePayload(i.PayloadJson)
                        })
                        .Cast<object>()
                        .ToList();

                    if (flat.Count == 0)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No {entityType} items found for vertical '{vertical}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(flat);
                }
                catch (InvocationException ie)
                {
                    var httpEx = ie.InnerException as HttpRequestException;
                    var status = httpEx?.StatusCode ?? HttpStatusCode.InternalServerError;
                    var pd = new ProblemDetails
                    {
                        Title = status == HttpStatusCode.NotFound ? "Not Found" : "Search Error",
                        Detail = ie.Message,
                        Status = (int)status
                    };
                    return TypedResults.Problem(pd.Title, pd.Detail, pd.Status);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Search failed for {EntityType}", entityType);
                    var pd = new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    };
                    return TypedResults.Problem(pd.Title, pd.Detail, pd.Status);
                }
            });

            group.MapGet($"/{routeSegment}/{{id}}", async Task<Results<
                    Ok<LandingBackOfficeIndex>,
                    NotFound<ProblemDetails>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>
                > (
                    string id,
                    [FromServices] ISearchService searchSvc,
                    [FromServices] ILoggerFactory loggerFactory,
                    CancellationToken ct
                ) =>
            {
                var logger = loggerFactory.CreateLogger("BackOfficeEndpoints");
                try
                {
                    logger.LogInformation("Retrieving {Id}", id);
                    var doc = await searchSvc.GetByIdAsync<LandingBackOfficeIndex>(IndexNames.LandingBackOfficeIndex, id);

                    if (doc == null || doc.Vertical != vertical || doc.EntityType != entityType)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"{entityType} '{id}' not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(doc);
                }
                catch (InvocationException ie)
                {
                    var httpEx = ie.InnerException as HttpRequestException;
                    var status = httpEx?.StatusCode ?? HttpStatusCode.InternalServerError;
                    var pd = new ProblemDetails
                    {
                        Title = status == HttpStatusCode.BadRequest ? "Bad Request"
                                 : status == HttpStatusCode.NotFound ? "Not Found"
                                 : "Dapr Invocation Error",
                        Detail = ie.Message,
                        Status = (int)status
                    };
                    return status == HttpStatusCode.BadRequest
                        ? TypedResults.BadRequest(pd)
                        : status == HttpStatusCode.NotFound
                            ? TypedResults.NotFound(pd)
                            : TypedResults.Problem(pd.Title, pd.Detail, pd.Status);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GetById failed for {Id}", id);
                    var pd = new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    };
                    return TypedResults.Problem(pd.Title, pd.Detail, pd.Status);
                }
            });

            group.MapDelete($"/{routeSegment}/{{id}}", async Task<Results<
                    NoContent,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>
                > (
                    string id,
                    [FromServices] IBackOfficeService<LandingBackOfficeIndex> stateSvc,
                    [FromServices] ILoggerFactory loggerFactory,
                    [FromServices] DaprClient dapr,
                    CancellationToken ct
                ) =>
            {
                var logger = loggerFactory.CreateLogger("BackOfficeEndpoints");
                try
                {
                    logger.LogInformation("Deleting {Id}", id);
                    await stateSvc.DeleteState(id, ct);

                    var msg = new IndexMessage
                    {
                        Vertical = IndexNames.LandingBackOfficeIndex,
                        Action = "Delete",
                        DeleteKey = id
                    };
                    await dapr.PublishEventAsync(PubSubName, PubSubTopics.IndexUpdates, msg, ct);
                    logger.LogInformation("Deleted and queued IndexMessage for {Id}", id);

                    return TypedResults.NoContent();
                }
                catch (InvocationException ie)
                {
                    var httpEx = ie.InnerException as HttpRequestException;
                    var status = httpEx?.StatusCode ?? HttpStatusCode.InternalServerError;
                    var pd = new ProblemDetails
                    {
                        Title = status == HttpStatusCode.NotFound ? "Not Found" : "Delete Error",
                        Detail = ie.Message,
                        Status = (int)status
                    };
                    return status == HttpStatusCode.NotFound
                        ? TypedResults.NotFound(pd)
                        : TypedResults.Problem(pd.Title, pd.Detail, pd.Status);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Delete failed for {Id}", id);
                    var pd = new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    };
                    return TypedResults.Problem(pd.Title, pd.Detail, pd.Status);
                }
            });

            return group;
        }

        private static List<object> BuildHierarchy(List<LandingBackOfficeIndex> items, string? parentId)
        {
            return items
                .Where(i => i.ParentId == parentId)
                .Select(i => new {
                    i.Id,
                    i.Title,
                    i.ParentId,
                    i.Vertical,
                    i.EntityType,
                    i.Order,
                    i.RediectUrl,
                    i.ImageUrl,
                    i.IsActive,
                    Children = BuildHierarchy(items, i.Id)
                })
                .ToList<object>();
        }
    }
}
