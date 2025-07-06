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

            group.MapPost("/createdailytopic", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            DailyTopic topic,
             [FromServices] IV2ContentDailyService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
        ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (userClaim == null)
                    {
                        return TypedResults.Forbid();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    var name = userData.GetProperty("name").GetString();

                    if (string.IsNullOrWhiteSpace(topic.TopicName))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "TopicName is required."
                        });
                    }

                    topic.Id = topic.Id == Guid.Empty ? Guid.NewGuid() : topic.Id;

                    await service.AddDailyTopicAsync(topic, cancellationToken);
                    return TypedResults.Ok("Daily topic created successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to create daily topic", ex.Message);
                }
            })
        .WithName("CreateDailyTopic")
        .WithTags("DailyTopic")
        .WithSummary("Create a daily topic (Authorized)")
        .WithDescription("Creates a daily topic with user authentication")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/dailytopicById", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                DailyTopic topic,
                [FromServices] IV2ContentDailyService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(topic.TopicName))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "TopicName is required."
                        });
                    }
                    topic.Id = topic.Id == Guid.Empty ? Guid.NewGuid() : topic.Id;

                    await service.AddDailyTopicAsync(topic, cancellationToken);
                    return TypedResults.Ok("Daily topic created successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to create daily topic", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("CreateDailyTopicById")
            .WithTags("DailyTopic")
            .WithSummary("Create a daily topic by explicit ID (no auth)")
            .WithDescription("Creates a daily topic using payload-provided ID and name without requiring authorization.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/dailytopics", async Task<Results<Ok<List<DailyTopic>>, ProblemHttpResult>> (
    [FromServices] IV2ContentDailyService service,
    CancellationToken cancellationToken) =>
            {
                try
                {
                    var topics = await service.GetAllDailyTopicsAsync(cancellationToken);
                    return TypedResults.Ok(topics);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to get daily topics", ex.Message);
                }
            })
.WithName("GetAllDailyTopics")
.WithTags("DailyTopic")
.WithSummary("Get all active daily topics")
.Produces<List<DailyTopic>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/updatedailytopic", async Task<Results<
    Ok<string>,
    ForbidHttpResult,
    BadRequest<ProblemDetails>,
    NotFound<ProblemDetails>,
    ProblemHttpResult>>
(
    DailyTopic topic,
    [FromServices] IV2ContentDailyService service,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
            {
                try
                {
                    // Validate user claim
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (userClaim == null)
                    {
                        return TypedResults.Forbid();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    var name = userData.GetProperty("name").GetString();

                    // Validate input
                    if (topic.Id == Guid.Empty || string.IsNullOrWhiteSpace(topic.TopicName))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "TopicName and valid Id are required."
                        });
                    }

                    var updated = await service.UpdateDailyTopicAsync(topic, cancellationToken);
                    if (!updated)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No topic found with ID {topic.Id}"
                        });
                    }

                    return TypedResults.Ok($"Daily topic updated successfully ");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to update daily topic", ex.Message);
                }
            })
.WithName("UpdateDailyTopic")
.WithTags("DailyTopic")
.WithSummary("Update a daily topic (Authorized)")
.WithDescription("Updates a daily topic using authenticated user claims (Drupal or AAD)")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/dailytopicupdateid", async Task<Results<
    Ok<string>,
    NotFound<ProblemDetails>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    DailyTopic topic,
    [FromServices] IV2ContentDailyService service,
    CancellationToken cancellationToken
) =>
            {
                try
                {
                    if (topic.Id == Guid.Empty || string.IsNullOrWhiteSpace(topic.TopicName))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "TopicName and valid Id are required."
                        });
                    }

                    var updated = await service.UpdateDailyTopicAsync(topic, cancellationToken);
                    if (!updated)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No topic found with ID {topic.Id}"
                        });
                    }

                    return TypedResults.Ok("Daily topic updated successfully (no auth).");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to update daily topic", ex.Message);
                }
            })
.ExcludeFromDescription()
.WithName("UpdateDailyTopicById")
.WithTags("DailyTopic")
.WithSummary("Update a daily topic (no auth)")
.WithDescription("Updates a daily topic without requiring authorization. Typically used for internal service calls.")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            group.MapPut("/publishstatus", async Task<Results<
          Ok<string>,
          ForbidHttpResult,
          NotFound<ProblemDetails>,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
      (
          DailyTopic dto,
          HttpContext httpContext,
          [FromServices] IV2ContentDailyService service,
          CancellationToken cancellationToken
      ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (userClaim == null)
                    {
                        return TypedResults.Forbid();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    var name = userData.GetProperty("name").GetString();

                    if (dto.Id == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "A valid non-empty Id is required."
                        });
                    }

                    var success = await service.UpdatePublishStatusAsync(dto.Id, dto.IsPublished, cancellationToken);
                    if (!success)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No topic found with ID {dto.Id}"
                        });
                    }

                    return TypedResults.Ok($"Topic {(dto.IsPublished ? "published" : "unpublished")}.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to update publish status", ex.Message);
                }
            })
      .RequireAuthorization()
      .WithName("UpdateDailyTopicPublishStatusWithAuth")
      .WithTags("DailyTopic")
      .WithSummary("Update publish/unpublish status of a topic (With Auth)")
      .WithDescription("Requires authentication and updates the IsPublished field. Tracks the user who made the change.")
      .Produces<string>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/publishstatusbyid", async Task<Results<
    Ok<string>,
    NotFound<ProblemDetails>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    DailyTopic dto,
    [FromServices] IV2ContentDailyService service,
    CancellationToken cancellationToken
) =>
            {
                try
                {
                    if (dto.Id == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "A valid non-empty Id is required."
                        });
                    }

                    var success = await service.UpdatePublishStatusAsync(dto.Id, dto.IsPublished, cancellationToken);
                    if (!success)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No topic found with ID {dto.Id}"
                        });
                    }

                    return TypedResults.Ok($"Topic {(dto.IsPublished ? "published" : "unpublished")} successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to update publish status", ex.Message);
                }
            })
.ExcludeFromDescription()
.WithName("UpdateDailyTopicPublishStatusPublic")
.WithTags("DailyTopic")
.WithSummary("Update publish/unpublish status of a topic (No Auth)")
.WithDescription("Used internally to toggle the IsPublished field without requiring authentication.")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/dailytopic/{id:guid}", async Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>> (
    Guid id,
    [FromServices] IV2ContentDailyService service,
    CancellationToken cancellationToken) =>
            {
                try
                {
                    var success = await service.DeleteDailyTopicAsync(id, cancellationToken);
                    if (!success)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Daily topic with ID {id} not found."
                        });
                    }

                    return TypedResults.Ok("Daily topic soft-deleted successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to soft-delete daily topic", ex.Message);
                }
            })
.WithName("SoftDeleteDailyTopic")
.WithTags("DailyTopic")
.WithSummary("Soft delete a daily topic by ID")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
