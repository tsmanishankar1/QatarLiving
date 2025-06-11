// QLN.Common.Infrastructure.CustomEndpoints/BackOfficeMasterEndpoints.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IBackOfficeService;
using QLN.Common.Infrastructure.IService.ISearchService;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Common.Infrastructure.CustomEndpoints
{
    public static class BackOfficeMasterEndpoints
    {
        /// <summary>
        /// Maps CRUD + pub-sub endpoints for any back-office master entity.
        /// Reads from the search index; writes to Dapr state + publishes IndexMessage.
        /// Includes detailed error and exception handling.
        /// </summary>
        public static RouteGroupBuilder MapBackOfficeMasterEndpoints(
            this RouteGroupBuilder group,
            string vertical,
            string routeSegment,
            string entityType)
        {
            group.MapPost($"/{routeSegment}",
                async Task<Results<
                    Ok<string>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>> (
                    BackofficemasterIndex doc,
                    IBackOfficeService<BackofficemasterIndex> stateSvc,
                    DaprClient dapr,
                    CancellationToken ct
                ) =>
                {
                    if (doc == null)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = "Request body must contain a BackofficemasterIndex object.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    doc.Vertical = vertical;
                    doc.EntityType = entityType;
                    if (string.IsNullOrWhiteSpace(doc.Id))
                        doc.Id = $"{vertical}-{entityType}-{Guid.NewGuid():N}".Substring(0, 8);

                    try
                    {
                        await stateSvc.UpsertState(doc, ct);
                        var msg = new IndexMessage
                        {
                            Vertical = vertical,
                            Action = "Upsert",
                            UpsertRequest = new CommonIndexRequest
                            {
                                VerticalName = ConstantValues.backofficemaster,
                                MasterItem = doc
                            }
                        };
                        await dapr.PublishEventAsync(
                            PubSubName,
                            PubSubTopics.IndexUpdates,
                            msg,
                            ct
                        );

                        return TypedResults.Ok("Record saved and queued for indexing.");
                    }
                    catch (OperationCanceledException)
                    {
                        return TypedResults.Problem(
                            title: "Request Cancelled",
                            detail: "The operation was cancelled.",
                            statusCode: StatusCodes.Status499ClientClosedRequest
                        );
                    }
                    catch (ArgumentException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Argument",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    catch (DaprException ex)
                    {
                        return TypedResults.Problem(
                            title: "Pub/Sub Error",
                            detail: $"Failed to publish indexing message: {ex.Message}",
                            statusCode: StatusCodes.Status502BadGateway
                        );
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                    }
                })
            .WithName($"Upsert_{vertical}_{entityType}")
            .WithSummary($"Upsert a single {entityType}")
            .WithDescription($"Write a {entityType} to state store and enqueue it for indexing.");

            group.MapGet($"/{routeSegment}", async Task<IResult> (
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
                var searchReq = new CommonSearchRequest
                {
                    Top = 100,
                    Filters = new Dictionary<string, object>
                    {
                        { "Vertical",   vertical },
                        { "EntityType", entityType }
                    }
                };

                try
                {
                    CommonSearchResponse response = await searchSvc.SearchAsync(ConstantValues.backofficemaster, searchReq);
                    var list = response.MasterItems ?? new List<BackofficemasterIndex>();

                    var filtered = list
                        .Where(d => d.Vertical == vertical && d.EntityType == entityType)
                        .ToList();

                    return TypedResults.Ok(filtered);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Arguments",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (RequestFailedException ex)
                {
                    return TypedResults.Problem(
                        title: "Search Service Error",
                        detail: $"Azure Search failed: {ex.Message}",
                        statusCode: StatusCodes.Status502BadGateway
                    );
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName($"GetAll_{vertical}_{entityType}")
            .WithSummary($"Get all {entityType}")
            .WithDescription($"Retrieves all {entityType} documents for vertical '{vertical}'.")
            .Produces<IEnumerable<BackofficemasterIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet($"/{routeSegment}/{{id}}",
                async Task<Results<
                    Ok<BackofficemasterIndex>,
                    BadRequest<ProblemDetails>,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>> (
                    string id,
                    ISearchService searchSvc,
                    CancellationToken ct
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(id))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = "Id must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    try
                    {
                        var doc = await searchSvc.GetByIdAsync<BackofficemasterIndex>(
                            backofficemaster, id);

                        if (doc == null
                         || doc.Vertical != vertical
                         || doc.EntityType != entityType)
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
                    catch (ArgumentException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Arguments",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    catch (RequestFailedException ex)
                    {
                        return TypedResults.Problem(
                            title: "Search Service Error",
                            detail: $"Azure Search lookup failed: {ex.Message}",
                            statusCode: StatusCodes.Status502BadGateway
                        );
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                    }
                })
            .WithName($"GetById_{vertical}_{entityType}")
            .WithSummary($"Get single published {entityType} by Id")
            .WithDescription($"Retrieve one published {entityType} by Id for vertical '{vertical}'");

            group.MapDelete($"/{routeSegment}/{{id}}",
                async Task<Results<
                    NoContent,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>> (
                    string id,
                    IBackOfficeService<BackofficemasterIndex> stateSvc,
                    DaprClient dapr,
                    CancellationToken ct
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(id))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = "Id must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    try
                    {
                        await stateSvc.DeleteState(id, ct);

                        var msg = new IndexMessage
                        {
                            Vertical = vertical,
                            Action = "Delete",
                            DeleteKey = id
                        };
                        await dapr.PublishEventAsync(
                            ConstantValues.PubSubName,
                            PubSubTopics.IndexUpdates,
                            msg,
                            ct
                        );

                        return TypedResults.NoContent();
                    }
                    catch (OperationCanceledException)
                    {
                        return TypedResults.Problem(
                            title: "Request Cancelled",
                            detail: "The operation was cancelled.",
                            statusCode: StatusCodes.Status499ClientClosedRequest
                        );
                    }
                    catch (ArgumentException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Arguments",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    catch (DaprException ex)
                    {
                        return TypedResults.Problem(
                            title: "Pub/Sub Error",
                            detail: $"Failed to publish delete: {ex.Message}",
                            statusCode: StatusCodes.Status502BadGateway
                        );
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                    }
                })
            .WithName($"Delete_{vertical}_{entityType}")
            .WithSummary($"Delete a single {entityType} by Id")
            .WithDescription($"Delete one {entityType} from state store and enqueue its removal from index.");

            return group;
        }
    }
}
