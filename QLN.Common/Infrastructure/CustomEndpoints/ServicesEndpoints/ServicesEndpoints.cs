using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

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
        public static RouteGroupBuilder MapServiceEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/dashboard-with-ads", async Task<IResult> (
                HttpContext context,
                [FromServices]IServicesService service,
                CancellationToken token) =>
            {
                var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var uid = userData.GetProperty("uid").GetString();
                if (uid == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the JWT token.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetDashboardAndAds(uid, token);

                    if ((result?.PublishedAds?.Any() != true) &&
                        (result?.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No ads were found for user ID '{uid}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested service ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
             .WithName("GetServiceDashboardWithAds")
             .WithTags("Services")
             .WithSummary("Get all service ads and dashboard (JWT)")
             .WithDescription("Returns both published/unpublished service ads and dashboard metrics for a given user ID from JWT.")
             .RequireAuthorization()
             .Produces<ServiceDashboardWithAdsDto>(StatusCodes.Status200OK)
             .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
             .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/dashboard-with-ads/byId", async Task<IResult> (
                [FromQuery] string userId,
                [FromServices]IServicesService service,
                CancellationToken token) =>
            {
                if (userId == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetDashboardAndAds(userId, token);

                    if ((result?.PublishedAds?.Any() != true) &&
                        (result?.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No ads were found for user ID '{userId}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested service ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetServiceDashboardWithAdsById")
            .WithTags("Services")
            .WithSummary("Get all service ads and dashboard (by userId)")
            .WithDescription("Returns both published/unpublished service ads and dashboard metrics for a given user ID (from query).")
            .Produces<ServiceDashboardWithAdsDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .ExcludeFromDescription();
            return group;
        }

    }
}
