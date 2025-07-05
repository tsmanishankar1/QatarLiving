using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using FirebaseAdmin.Auth;
using QLN.Common.Infrastructure.CustomException;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2DailyEndpoint
    {
        public static RouteGroupBuilder MapDailyTopicEndpoints(this RouteGroupBuilder group)
        {
            // POST /dailyTopics
            group.MapPost(
                "/topsection",
                async Task<Results<
                    Created<string>,
                    ForbidHttpResult,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
                (
                    [FromBody] DailyTopSectionSlot dto,
                    [FromServices] IV2ContentDailyService service,
                    HttpContext httpContext,
                    CancellationToken ct
                ) =>
                {
                    try
                    {
                        var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                        if (string.IsNullOrEmpty(userClaim))
                        {
                            return TypedResults.Forbid();
                        }
                        var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                        var userId = userData.GetProperty("uid").GetString();
                        if(userId == null)
                        {
                            return TypedResults.Forbid();
                        }
                        var result = await service.UpsertSlotAsync(userId, dto, ct);
                        return TypedResults.Created("/dailySlots", result);
                    }
                    catch (DaprServiceException ex)
                    {
                        if (ex.StatusCode == 400)
                        {
                            var pd = JsonSerializer.Deserialize<ProblemDetails>(ex.ResponseBody)
                                     ?? new ProblemDetails { Title = "Bad Request", Detail = ex.ResponseBody };
                            pd.Status = 400;
                            return TypedResults.BadRequest(pd);
                        }
                        return TypedResults.Problem(
                            title: $"Upstream error {(ex.StatusCode)}",
                            detail: ex.ResponseBody,
                            statusCode: ex.StatusCode,
                            instance: httpContext.Request.Path
                        );
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Slot Out of Range",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem("Internal Server Error", ex.Message);
                    }
                })
                .WithName("CreateOrUpdateSlotByToken")
                .WithTags("DailySlots")
                .WithSummary("Create or update a daily slot using token")
                .WithDescription("Uses JWT to extract userId and sets CreatedAt, updates slot info.");

            group.MapPost(
                "/topsection/{userId}",
                async Task<Results<
                    Created<string>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
                (
                    [FromBody] DailyTopSectionSlot dto,
                    [FromRoute] string userId,
                    [FromServices] IV2ContentDailyService service,
                    CancellationToken ct
                ) =>
                {
                    try
                    {
                        var result = await service.UpsertSlotAsync(userId, dto, ct);
                        return TypedResults.Created("/dailySlots", result);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Slot Out of Range",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem("Internal Server Error", ex.Message);
                    }
                })
                .WithName("CreateOrUpdateSlotInternal")
                .WithTags("DailySlots")
                .ExcludeFromDescription();

            // GET /dailySlots
            group.MapGet(
                "/topsection",
                async Task<Results<
                    Ok<List<DailyTopSectionSlot>>,
                    ProblemHttpResult>>
                (
                    [FromServices] IV2ContentDailyService service,
                    CancellationToken cancellationToken
                ) =>
                {
                    try
                    {
                        var all = await service.GetAllSlotsAsync(cancellationToken);
                        return TypedResults.Ok(all);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem("Error retrieving slots", ex.Message);
                    }
                }
            )
            .WithName("GetAllDailySlots")
            .WithTags("DailySlots")
            .WithSummary("Fetch all 9 daily slots (placeholders for empty ones)")
            .Produces<List<DailyTopSectionSlot>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
