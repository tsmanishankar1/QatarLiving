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
using System.Net.Http;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2DailyEndpoint
    {
        public static RouteGroupBuilder MapDailyTopicEndpoints(this RouteGroupBuilder group)
        {
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
                .WithTags("DailyLivingBO")
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
                .WithTags("DailyLivingBO")
                .ExcludeFromDescription();


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
            .WithTags("DailyLivingBO")
            .WithSummary("Fetch all 9 daily slots (placeholders for empty ones)")
            .Produces<List<DailyTopSectionSlot>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/topic/content",
               async Task<Results<
                   Created<string>,
                   ForbidHttpResult,
                   BadRequest<ProblemDetails>,
                   ProblemHttpResult>>
               (
                   [FromBody] DailyTopicContent dto,
                   [FromServices] IV2ContentDailyService service,
                   HttpContext httpContext,
                   CancellationToken ct
               ) =>
               {
                   try
                   {
                       var userClaim = httpContext.User.Claims
                                       .FirstOrDefault(c => c.Type == "user")?.Value;
                       if (string.IsNullOrEmpty(userClaim))
                           return TypedResults.Forbid();

                       var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                       var userId = userData.GetProperty("uid").GetString()!;

                       dto.CreatedBy = userId;
                       dto.UpdatedBy = userId;
                       dto.CreatedAt = DateTime.UtcNow;
                       dto.UpdatedAt = DateTime.UtcNow;
                       dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;

                       var result = await service.CreateContentAsync(userId, dto, ct);

                       return TypedResults.Created($"/topic/content/{userId}", result);
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
                   catch (InvalidDataException ex)
                   {
                       return TypedResults.BadRequest(new ProblemDetails
                       {
                           Title = "Invalid Data",
                           Detail = ex.Message,
                           Status = StatusCodes.Status400BadRequest
                       });
                   }
                   catch (DaprServiceException ex)
                   {
                       var pd = new ProblemDetails
                       {
                           Title = $"Upstream service error ({ex.StatusCode})",
                           Detail = ex.ResponseBody,
                           Status = ex.StatusCode
                       };
                       return TypedResults.Problem(
                           title: pd.Title,
                           detail: pd.Detail,
                           statusCode: pd.Status,
                           instance: httpContext.Request.Path
                       );
                   }
                   catch (Exception ex)
                   {
                       return TypedResults.Problem(
                           title: "Internal Server Error",
                           detail: ex.Message
                       );
                   }
               })
               .WithName("CreateDailyTopicContentByToken")
               .WithTags("DailyLivingBO")
               .WithSummary("Create or update content in a topic slot (uses JWT to extract userId)")
               .RequireAuthorization();


            group.MapPost(
                "/topic/contentbyid/{userId}",
                async Task<Results<
                    Created<string>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
                (
                    [FromRoute] string userId,
                    [FromBody] DailyTopicContent dto,
                    [FromServices] IV2ContentDailyService service,
                    CancellationToken ct
                ) =>
                {
                    try
                    {
                        dto.CreatedAt = DateTime.UtcNow;
                        dto.UpdatedAt = DateTime.UtcNow;
                        dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;

                        var result = await service.CreateContentAsync(userId, dto, ct);
                        return TypedResults.Created($"/topic/content/{userId}", result);
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
                    catch (InvalidDataException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    catch (DaprServiceException ex)
                    {
                        var pd = new ProblemDetails
                        {
                            Title = $"Upstream service error ({ex.StatusCode})",
                            Detail = ex.ResponseBody,
                            Status = ex.StatusCode
                        };
                        return TypedResults.Problem(
                            title: pd.Title,
                            detail: pd.Detail,
                            statusCode: pd.Status
                        );
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message
                        );
                    }
                })
            .WithName("CreateDailyTopicContentInternal")
            .WithTags("DailyLivingBO")
            .ExcludeFromDescription();

            group.MapGet(
                "/topic/content",
                async Task<Results<
                    Ok<List<DailyTopicContent>>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
                (
                    [FromHeader(Name = "TopicId")] Guid topicId,
                    [FromServices] IV2ContentDailyService service,
                    CancellationToken ct
                ) =>
                {
                    if (topicId == Guid.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Missing Header",
                            Detail = "TopicId header is required",
                            Status = StatusCodes.Status400BadRequest
                        });

                    try
                    {
                        var list = await service.GetSlotsByTopicAsync(topicId, ct);
                        return TypedResults.Ok(list);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem("Error retrieving topic slots", ex.Message);
                    }
                })
                .WithName("GetDailyTopicContentByTopic")
                .WithTags("DailyLivingBO")
                .WithSummary("Fetch all filled slots for a given topic (TopicId in header)")
                .Produces<List<DailyTopicContent>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/topic/content/reorder", async Task<Results<
                    Ok<string>,
                    ForbidHttpResult,
                    BadRequest<ProblemDetails>,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>>
                (
                    [FromBody] ReorderDailyTopicContentDto dto,
                    [FromServices] IV2ContentDailyService svc,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
            {
                try
                {
                    if (dto.FromSlot < 1 || dto.FromSlot > 9 || dto.ToSlot < 1 || dto.ToSlot > 9)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Slots must be between 1 and 9.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var userClaim = ctx.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (userClaim is null) return TypedResults.Forbid();

                    var ud = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    dto.UserId = ud.GetProperty("uid").GetString()!;
                    dto.AuthorName = ud.GetProperty("name").GetString();

                    var result = await svc.ReorderSlotsAsync(dto.UserId, dto, ct);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("ReorderDailyTopicSlots")
            .WithTags("DailyLivingBO")
            .WithSummary("Shift a piece of content from one slot to another, reordering intervening items")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/topic/content/reorderbyid/{userId}", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                [FromBody] ReorderDailyTopicContentDto dto,
                [FromRoute] string userId,
                [FromServices] IV2ContentDailyService svc,
                HttpContext ctx,
                CancellationToken ct
            ) =>
            {
                try
                {
                    if (dto.FromSlot < 1 || dto.FromSlot > 9 || dto.ToSlot < 1 || dto.ToSlot > 9)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Slots must be between 1 and 9.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    dto.UserId = userId;

                    var result = await svc.ReorderSlotsAsync(userId, dto, ct);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("ReorderDailyTopicSlotsbyid")
            .WithTags("DailyLivingBO")
            .WithSummary("Shift a piece of content from one slot to another, reordering intervening items")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .ExcludeFromDescription(); 

            group.MapDelete("/topic/content/{contentId:guid}", async Task<Results<
                    Ok<string>,
                    ForbidHttpResult,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>>
                (
                    [FromRoute] Guid contentId,
                    [FromServices] IV2ContentDailyService svc,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
            {
                try
                {
                    var result = await svc.DeleteContentAsync(contentId, ct);
                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("DeleteDailyTopicContent")
            .WithTags("DailyLivingBO")
            .WithSummary("Delete a daily topic content item and shift remaining slots up")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
