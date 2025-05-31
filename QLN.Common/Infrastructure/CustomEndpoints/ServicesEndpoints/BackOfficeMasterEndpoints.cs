// File: QLN.Common.Infrastructure.CustomEndpoints.BackOfficeMasterEndpoints.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;       // for RouteGroupBuilder
using Microsoft.AspNetCore.Http;          // for IResult, TypedResults, StatusCodes
using Microsoft.AspNetCore.Http.HttpResults; // for Results<...> types
using Microsoft.AspNetCore.Mvc;            // for [FromServices]
using Microsoft.AspNetCore.Routing;       // for RouteGroupBuilder
using QLN.Common.DTO_s;                   // for BackofficemasterIndex, CommonIndexRequest, SearchRequest, CommonResponse
using QLN.Common.Infrastructure.IService.ISearchService;  // for ISearchService

namespace QLN.Common.Infrastructure.CustomEndpoints
{
    /// <summary>
    /// Provides extension methods to map Back‐Office master‐data POST/GET endpoints
    /// for any vertical and entityType. Each route stamps DocVertical and EntityType
    /// on incoming BackofficemasterIndex DTOs, then calls ISearchService for upload/search/getById.
    /// </summary>
    public static class BackOfficeMasterEndpoints
    {
        /// <summary>
        /// Maps three endpoints under {routeSegment} on the given group:
        ///   • POST  /{routeSegment}           → Upsert (create or update) a BackofficemasterIndex doc  
        ///   • GET   /{routeSegment}           → Retrieve all BackofficemasterIndex docs filtered by vertical & entityType  
        ///   • GET   /{routeSegment}/{id}      → Retrieve a single BackofficemasterIndex by its composite Id  
        ///
        /// Each BackofficemasterIndex is stamped with:
        ///   d.Vertical   = vertical;
        ///   d.EntityType = entityType;
        ///
        /// The actual Azure Search index name is hard‐coded to “backofficemaster” inside the CommonIndexRequest.
        /// </summary>
        /// <param name="group">The RouteGroupBuilder under which to map these endpoints (e.g. app.MapGroup("/api/services")).</param>
        /// <param name="vertical">Logical vertical name (e.g. "services", "classifieds").</param>
        /// <param name="routeSegment">The path segment (e.g. "featured-categories", "seasonal-picks").</param>
        /// <param name="entityType">The master‐data entity type (e.g. "FeaturedCategory", "SeasonalPick").</param>
        /// <returns>The same RouteGroupBuilder, for chaining.</returns>
        public static RouteGroupBuilder MapBackOfficeMasterEndpoints(
            this RouteGroupBuilder group,
            string vertical,
            string routeSegment,
            string entityType)
        {
            // ──────────────────────────────────────────────────────────
            // 1) POST /{routeSegment}
            //    Upsert a single BackofficemasterIndex doc
            // ──────────────────────────────────────────────────────────
            group.MapPost($"/{routeSegment}", async Task<Results<
                    Ok<object>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
                (
                    BackofficemasterIndex doc,
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
                if (doc == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Request body must contain a BackofficemasterIndex object.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                // Stamp Vertical & EntityType
                doc.Vertical = vertical;
                doc.EntityType = entityType;

                // If Id is null or empty, generate a new composite Id:
                // e.g. "services-FeaturedCategory-XXXXXXXX"
                if (string.IsNullOrWhiteSpace(doc.Id))
                {
                    var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8);
                    doc.Id = $"{doc.Vertical}-{doc.EntityType}-{shortGuid}";
                }

                var req = new CommonIndexRequest
                {
                    VerticalName = "backofficemaster",
                    MasterItem = doc
                };

                try
                {
                    // Calls ExternalSearchService.UploadAsync(req) via the injected ISearchService
                    await searchSvc.UploadAsync(req);
                    return TypedResults.Ok<object>(new { IndexedCount = 1 });
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
            .WithTags("LandingPageMaster")
            .WithSummary($"Upsert a single {entityType}")
            .WithDescription($"Upserts one {entityType} document into the backoffice-master search index.");

            // ──────────────────────────────────────────────────────────
            // 2) GET /{routeSegment}
            //    Retrieve all docs filtered by vertical & entityType
            // ──────────────────────────────────────────────────────────
            group.MapGet($"/{routeSegment}", async Task<IResult> (
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
                var searchReq = new SearchRequest
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
                    // Calls ExternalSearchService.SearchAsync("backofficemaster", searchReq)
                    CommonResponse response = await searchSvc.SearchAsync("backofficemaster", searchReq);
                    var list = response.MasterItems ?? new List<BackofficemasterIndex>();

                    // Double‐check that Vertical & EntityType match, just in case:
                    var filtered = list
                        .Where(d => d.Vertical == vertical && d.EntityType == entityType)
                        .ToList();

                    return TypedResults.Ok(filtered);
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
            .WithTags("LandingPageMaster")
            .WithSummary($"Get all {entityType}")
            .WithDescription($"Retrieves all {entityType} documents for vertical '{vertical}'.")
            .Produces<IEnumerable<BackofficemasterIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // ──────────────────────────────────────────────────────────
            // 3) GET /{routeSegment}/{id}
            //    Retrieve a single doc by its composite Id
            // ──────────────────────────────────────────────────────────
            group.MapGet($"/{routeSegment}/{{id}}", async Task<IResult> (
                    string id,
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Id must be provided in the URL.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    // Use the existing “GetByIdAsync<T>” helper in the external search service:
                    var doc = await searchSvc.GetByIdAsync<BackofficemasterIndex>("backofficemaster", id);

                    if (doc is null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"{entityType} with Id '{id}' not found under vertical '{vertical}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    // Optionally double‐check vertical/entityType fields, although GetByIdAsync
                    // should already fetch the exact document by its composite key:
                    if (doc.Vertical != vertical || doc.EntityType != entityType)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No {entityType} with Id '{id}' found under vertical '{vertical}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(doc);
                }
                catch (ArgumentNullException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
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
            .WithTags("LandingPageMaster")
            .WithSummary($"Get a single {entityType} by Id")
            .WithDescription($"Retrieves one {entityType} document by its Id for vertical '{vertical}'.")
            .Produces<BackofficemasterIndex>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
