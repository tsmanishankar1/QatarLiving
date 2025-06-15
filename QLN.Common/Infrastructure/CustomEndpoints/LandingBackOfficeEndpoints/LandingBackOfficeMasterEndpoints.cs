using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IBackOfficeService;
using QLN.Common.Infrastructure.IService.ISearchService;
using System.Text.Json;
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
            group.MapPost($"/{routeSegment}",
                async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
                    LandingBackOfficeRequestDto dto,
                    [FromServices] IBackOfficeService<LandingBackOfficeIndex> stateSvc,
                    [FromServices] ILoggerFactory loggerFactory,
                    DaprClient dapr,
                    CancellationToken ct
                ) =>
                {
                    var _logger = loggerFactory.CreateLogger("BackOfficeEndpoints");

                    if (dto == null)
                    {
                        _logger.LogWarning("POST {RouteSegment}: Request body was null", routeSegment);
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = "Request body must contain a LandingBackOfficeRequestDto.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    // Inject AdId into PayloadJson filters if present
                    if (!string.IsNullOrWhiteSpace(dto.AdId))
                    {
                        if (dto.PayloadJson == null)
                            dto.PayloadJson = new CommonSearchRequest();
                        dto.PayloadJson.Filters["Id"] = dto.AdId;
                    }
                    string id = $"{vertical}-{entityType}-{Guid.NewGuid().ToString("N")[..8]}";
                    var doc = new LandingBackOfficeIndex
                    {
                        Id = id,
                        Vertical = vertical,
                        EntityType = entityType,
                        Title = dto.Title,
                        Description = dto.Description,
                        Order = dto.Order ?? 0,
                        ParentId = dto.ParentId,
                        IsActive = dto.IsActive,
                        RediectUrl = dto.RediectUrl,
                        ImageUrl = dto.ImageUrl,
                        ListingCount = dto.ListingCount,
                        RotationSeconds = dto.RotationSeconds,
                        AdId = dto.AdId
                    };

                    if (dto.PayloadJson != null)
                        doc.PayloadJson = JsonSerializer.Serialize(dto.PayloadJson);

                    try
                    {
                        _logger.LogInformation("Upserting document: {Id}", doc.Id);
                        await stateSvc.UpsertState(doc, ct);

                        var msg = new IndexMessage
                        {
                            Vertical = vertical,
                            Action = "Upsert",
                            UpsertRequest = new CommonIndexRequest
                            {
                                VerticalName = LandingBackOffice,
                                MasterItem = doc
                            }
                        };

                        await dapr.PublishEventAsync(PubSubName, PubSubTopics.IndexUpdates, msg, ct);
                        _logger.LogInformation("Successfully published IndexMessage for {Id}", doc.Id);

                        return TypedResults.Ok("Record saved and queued for indexing.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upsert or publish for {Id}", doc.Id);
                        return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
                    }
                });


            group.MapGet($"/{routeSegment}", async Task<IResult> (
                [FromServices] ISearchService searchSvc,
                [FromServices] ILoggerFactory loggerFactory,
                CancellationToken ct
            ) =>
            {
                var _logger = loggerFactory.CreateLogger("BackOfficeEndpoints");
                var searchReq = new CommonSearchRequest
                {
                    Top = 500,
                    Filters = new Dictionary<string, object>
                    {
                        { "Vertical", vertical },
                        { "EntityType", entityType }
                    }
                };

                try
                {
                    _logger.LogInformation("Searching for {EntityType} under {Vertical}", entityType, vertical);
                    var response = await searchSvc.SearchAsync(LandingBackOffice, searchReq);
                    var items = response.MasterItems ?? new List<LandingBackOfficeIndex>();

                    if (items.Any(x => !string.IsNullOrWhiteSpace(x.ParentId)))
                        return TypedResults.Ok(BuildHierarchy(items, null));

                    return TypedResults.Ok(items);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Search failed for {EntityType}", entityType);
                    return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
                }
            });

            group.MapGet($"/{routeSegment}/{{id}}",
                async Task<Results<Ok<LandingBackOfficeIndex>, NotFound<ProblemDetails>, ProblemHttpResult>> (
                    string id,
                    [FromServices] ISearchService searchSvc,
                    [FromServices] ILoggerFactory loggerFactory,
                    CancellationToken ct
                ) =>
                {
                    var _logger = loggerFactory.CreateLogger("BackOfficeEndpoints");
                    try
                    {
                        _logger.LogInformation("Retrieving {Id}", id);
                        var doc = await searchSvc.GetByIdAsync<LandingBackOfficeIndex>(LandingBackOffice, id);

                        if (doc == null || doc.Vertical != vertical || doc.EntityType != entityType)
                        {
                            _logger.LogWarning("Not found or mismatched: {Id}", id);
                            return TypedResults.NotFound(new ProblemDetails { Title = "Not Found", Detail = $"{entityType} '{id}' not found." });
                        }

                        return TypedResults.Ok(doc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "GetById failed for {Id}", id);
                        return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
                    }
                });

            group.MapDelete($"/{routeSegment}/{{id}}",
                async Task<Results<NoContent, ProblemHttpResult>> (
                    string id,
                    [FromServices] IBackOfficeService<LandingBackOfficeIndex> stateSvc,
                    [FromServices] ILoggerFactory loggerFactory,
                    DaprClient dapr,
                    CancellationToken ct
                ) =>
                {
                    var _logger = loggerFactory.CreateLogger("BackOfficeEndpoints");

                    try
                    {
                        _logger.LogInformation("Deleting {Id}", id);
                        await stateSvc.DeleteState(id, ct);

                        var msg = new IndexMessage
                        {
                            Vertical = LandingBackOffice,
                            Action = "Delete",
                            DeleteKey = id
                        };

                        await dapr.PublishEventAsync(PubSubName, PubSubTopics.IndexUpdates, msg, ct);
                        _logger.LogInformation("Successfully deleted and published for {Id}", id);

                        return TypedResults.NoContent();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Delete failed for {Id}", id);
                        return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
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
                }).ToList<object>();
        }
    }
}
