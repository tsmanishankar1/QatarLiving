using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ServicesEndpoints
{
    /// <summary>
    /// Provides extension methods to map “featured‐items” search endpoints:
    ///   • GET /services/featured-items       → returns all ServicesIndex where IsFeatured == true
    ///
    /// Each handler calls ISearchService.SearchAsync(vertical, SearchRequest { Filters = { "IsFeatured", true } }).
    /// </summary>
    public static class ServicesEndpoints
    {
        public static RouteGroupBuilder MapServicesFeaturedItemEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/featured-services", async Task<IResult> (
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
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
                    CommonSearchResponse response = await searchSvc.SearchAsync(
                        ConstantValues.Verticals.Services,
                        searchReq
                    );

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
