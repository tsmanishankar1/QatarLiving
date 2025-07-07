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
using QLN.Common.Infrastructure.DTO_s;

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
                        if (userId == null)
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
                .WithSummary("Create or update a daily living top section using jwt token")
                .WithDescription("Uses JWT to extract userId and sets CreatedAt, updates slot info and User can create and update the slot record")
                .RequireAuthorization();

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
            .WithSummary("Fetch all 9 daily slots of Daily Living Top section")
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
                    [FromQuery] Guid topicId,
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
                .WithSummary("Fetch all filled slots for a given daily living topic")
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
                    [FromBody] DailyTopicSlotReorderRequest req,
                    [FromServices] IV2ContentDailyService svc,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
            {
                try
                {
                    var userClaim = ctx.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (userClaim is null) return TypedResults.Forbid();

                    var ud = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    req.UserId = ud.GetProperty("uid").GetString()!;

                    var result = await svc.ReorderSlotsBatchAsync(req.UserId, req, ct);
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
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            group.MapPost("/topic/content/reorderbyid/{userId}", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                [FromBody] DailyTopicSlotReorderRequest req,
                [FromRoute] string userId,
                [FromServices] IV2ContentDailyService svc,
                HttpContext ctx,
                CancellationToken ct
            ) =>
            {
                try
                {
                    req.UserId = userId;

                    var result = await svc.ReorderSlotsBatchAsync(userId, req, ct);
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
        .WithTags("DailyLivingBO")
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
            .WithTags("DailyLivingBO")
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
            .WithTags("DailyLivingBO")
            .WithSummary("Get all daily topics")
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
            .WithTags("DailyLivingBO")
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
            .WithTags("DailyLivingBO")
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
          .WithName("UpdateDailyTopicPublishStatusWithAuth")
          .WithTags("DailyLivingBO")
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
            .WithTags("DailyLivingBO")
            .WithSummary("Soft delete a daily topic by ID")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet(
                "/topic/{topicId}/unusedarticles",
                async Task<Results<
                    Ok<List<V2NewsArticleDTO>>,
                    BadRequest<ProblemDetails>,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>>
                (
                    [FromRoute] Guid topicId,
                    [FromServices] IV2ContentDailyService service,
                    CancellationToken ct
                ) =>
                {
                    if (topicId == Guid.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid TopicId",
                            Detail = "TopicId cannot be empty."
                        });

                    try
                    {
                        var list = await service.GetUnusedNewsArticlesForTopicAsync(topicId, ct);

                        if (list == null || !list.Any())
                            return TypedResults.NotFound(new ProblemDetails
                            {
                                Title = "No Articles Found",
                                Detail = $"No unused articles for topic {topicId}."
                            });

                        return TypedResults.Ok(list);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message
                        });
                    }
                    catch (DaprServiceException ex)
                    {
                        return TypedResults.Problem(
                            title: $"Upstream error ({ex.StatusCode})",
                            detail: ex.ResponseBody,
                            statusCode: ex.StatusCode,
                            instance: $"/topic/{topicId}/unusedarticles"
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
                .WithName("GetUnusedNewsArticlesForTopic")
                .WithTags("DailyLivingBO")
                .WithSummary("Fetch news articles not yet used in a Daily Topic")
                .Produces<List<V2NewsArticleDTO>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status502BadGateway)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            group.MapGet(
                "/landing",
                async Task<Results<
                    Ok<ContentsDailyPageResponse>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
                (
                    [FromServices] IV2ContentDailyService service,
                    CancellationToken ct
                ) =>
                {
                    try
                    {
                        var response = await service.GetDailyLivingLandingAsync(ct);
                        return TypedResults.Ok(response);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Failed to build landing page",
                            detail: ex.Message
                        );
                    }
                })
                .WithName("GetDailyLivingLanding")
                .WithTags("DailyLivingBO")
                .WithSummary("Builds the daily-living landing payload")
                .Produces<ContentsDailyPageResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
