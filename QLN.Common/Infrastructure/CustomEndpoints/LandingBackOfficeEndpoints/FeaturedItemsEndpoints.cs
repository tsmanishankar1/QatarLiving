// File: QLN.Common.Infrastructure.CustomEndpoints.FeaturedSearchEndpoints.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;          // for RouteGroupBuilder
using Microsoft.AspNetCore.Http;             // for IResult, TypedResults, StatusCodes
using Microsoft.AspNetCore.Http.HttpResults; // for Results<...> types
using Microsoft.AspNetCore.Mvc;              // for [FromServices]
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;                      // for SearchRequest, CommonResponse, ServicesIndex, ClassifiedsIndex
using QLN.Common.Infrastructure.Constants;   // for BackOfficeConstants
using QLN.Common.Infrastructure.IService.ISearchService; // for ISearchService

namespace QLN.Common.Infrastructure.CustomEndpoints
{
    /// <summary>
    /// Provides extension methods to map “featured‐items” search endpoints:
    ///   • GET /services/featured-items       → returns all ServicesIndex where IsFeatured == true
    ///   • GET /classifieds/featured-items    → returns all ClassifiedsIndex where IsFeatured == true
    ///
    /// Each handler calls ISearchService.SearchAsync(vertical, SearchRequest { Filters = { "IsFeatured", true } }).
    /// </summary>
    public static class FeaturedItemsEndpoints
    {
        /// <summary>
        /// Maps two GET endpoints under the provided group:
        ///   • GET /services/featured-items
        ///   • GET /classifieds/featured-items
        /// </summary>
        /// <param name="group">
        ///   A RouteGroupBuilder (e.g. app.MapGroup("/api/search")).
        /// </param>
        /// <returns>The same RouteGroupBuilder, for chaining.</returns>
        public static RouteGroupBuilder MapClassifiedsFeaturedItemEndpoint(this RouteGroupBuilder group)
        {
           
            group.MapGet("/featured-items", async Task<IResult> (
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
                var searchReq = new CommonSearchRequest
                {
                    Top = 100,
                    Filters = new Dictionary<string, object>
                   {
                        { "IsFeaturedItem",   true },
                        { "SubVertical", "Items" }
                    }
                };

                try
                {
                    CommonSearchResponse response = await searchSvc.SearchAsync(
                        ConstantValues.Verticals.Classifieds,
                        searchReq
                    );

                    var list = response.ClassifiedsItems ?? new List<ClassifiedsIndex>();

                    return TypedResults.Ok(list);
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
            .WithName($"GetFeatured_{ConstantValues.Verticals.Classifieds}_Items")
            .WithTags("Classified")
            .WithSummary("Get all featured classified items")
            .WithDescription("Fetches every ClassifiedsIndex document where IsFeatured = true.")
            .Produces<IEnumerable<ClassifiedsIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServicesFeaturedItemEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/featured-services", async Task<IResult> (
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
                // Build a SearchRequest that filters on IsFeatured == true
                var searchReq = new CommonSearchRequest
                {
                    Top = 100,
                    Filters = new Dictionary<string, object>
                    {
                        { "IsFeatured", true }
                    }
                };

                try
                {
                    // Call the search service under the "services" vertical
                    CommonSearchResponse response = await searchSvc.SearchAsync(
                        ConstantValues.Verticals.Services,
                        searchReq
                    );

                    // Extract the list of services from response.ServicesItems
                    var list = response.ServicesItems ?? new List<ServicesIndex>();

                    return TypedResults.Ok(list);
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
            .WithName($"GetFeatured_{ConstantValues.Verticals.Services}_Items")
            .WithTags("Services")
            .WithSummary("Get all featured service items")
            .WithDescription("Fetches every ServicesIndex document where IsFeatured = true.")
            .Produces<IEnumerable<ServicesIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

    }
}
