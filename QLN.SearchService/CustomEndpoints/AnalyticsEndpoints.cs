using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.ISearchService;
using System;

namespace QLN.SearchService.CustomEndpoints
{
    public static class AnalyticsEndpoints
    {
        public static RouteGroupBuilder MapAnalyticsEndpoints(this RouteGroupBuilder group)
        {
            const string TAG = "Analytics";

            // ─── GET SUMMARY ─────────────────────────────────────────────────
            group.MapGet("/getAnalytics/{section}/{entityId}", async (
                        [FromRoute] string section,
                        [FromRoute] string entityId,
                        IAnalyticsService svc,
                        ILoggerFactory logFactory
                    ) =>
            {
                var logger = logFactory.CreateLogger(TAG);
                try
                {
                    if (string.IsNullOrWhiteSpace(section)
                     || string.IsNullOrWhiteSpace(entityId))
                    {
                        throw new ArgumentException("Both 'section' and 'entityId' must be provided.");
                    }

                    var summary = await svc.GetAsync(section, entityId);
                    if (summary is null)
                    {
                        return Results.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No analytics for section='{section}', entityId='{entityId}'.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = $"/api/analytics/getAnalytics/{section}/{entityId}"
                        });
                    }

                    return Results.Ok(summary);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Bad request in GetAnalytics");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/analytics/getAnalytics/{section}/{entityId}"
                    });
                }
                catch (RequestFailedException ex)
                {
                    logger.LogError(ex, "Search index error in GetAnalytics");
                    var code = ex.Status is >= 400 and < 600
                               ? ex.Status
                               : StatusCodes.Status502BadGateway;
                    return Results.Problem(
                        title: "Search Index Error",
                        detail: ex.Message,
                        statusCode: code,
                        instance: $"/api/analytics/getAnalytics/{section}/{entityId}"
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in GetAnalytics");
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/analytics/getAnalytics/{section}/{entityId}"
                    );
                }
            })
                .WithName("GetAnalytics")
                .WithTags(TAG)
                .WithSummary("Gets the analytics summary for a given section and entity.")
                .Produces<AnalyticsIndex>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status502BadGateway)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // ─── POST/UPSERT ────────────────────────────────────────────────
            group.MapPost("/upsertAnalytics", async (
                        [FromBody] AnalyticsEventRequest req,
                        IAnalyticsService svc,
                        ILoggerFactory logFactory
                    ) =>
            {
                var logger = logFactory.CreateLogger(TAG);
                try
                {
                    if (req is null)
                        throw new ArgumentException("Request body cannot be null.");

                    await svc.UpsertAsync(req);
                    return Results.Accepted();
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Bad request in PostAnalytics");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = "/api/analytics/upsertAnalytics"
                    });
                }
                catch (RequestFailedException ex)
                {
                    logger.LogError(ex, "Search index error in PostAnalytics");
                    var code = ex.Status is >= 400 and < 600
                               ? ex.Status
                               : StatusCodes.Status502BadGateway;
                    return Results.Problem(
                        title: "Search Index Error",
                        detail: ex.Message,
                        statusCode: code,
                        instance: "/api/analytics"
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in PostAnalytics");
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: "/api/analytics"
                    );
                }
            })
                .WithName("PostAnalytics")
                .WithTags(TAG)
                .WithSummary("Records or increments analytics counters.")
                .Accepts<AnalyticsEventRequest>("application/json")
                .Produces(StatusCodes.Status202Accepted)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status502BadGateway)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
