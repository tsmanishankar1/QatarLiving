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

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2DailyEndpoint
    {
        public static RouteGroupBuilder MapDailyTopicEndpoints(this RouteGroupBuilder group)
        {
            // GET /dailyTopics
            group.MapGet("/dailyTopics", async Task<Results<
                    Ok<List<DailyTopSectionSlot>>,
                    ProblemHttpResult>>
            (
                [FromServices]IV2ContentDailyService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var topics = await service.GetAllDailyTopicsAsync(cancellationToken);
                    return TypedResults.Ok(topics);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Error retrieving daily topics", ex.Message);
                }
            })
            .WithName("GetAllDailyTopics")
            .WithTags("DailyTopics")
            .WithSummary("Get all Daily Topics")
            .WithDescription("Returns a list of all daily topics.")
            .Produces<List<DailyTopicContent>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // GET /dailyTopics/{id}
            group.MapGet("/dailyTopics/{id:guid}", async Task<Results<
                    Ok<DailyTopSectionSlot>,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>>
            (
                Guid id,
                [FromServices] IV2ContentDailyService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var topic = await service.GetDailyTopicByIdAsync(id, cancellationToken);
                    return topic is not null
                        ? TypedResults.Ok(topic)
                        : TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Daily topic '{id}' not found."
                        });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Error retrieving daily topic", ex.Message);
                }
            })
            .WithName("GetDailyTopicById")
            .WithTags("DailyTopics")
            .WithSummary("Get Daily Topic by ID")
            .WithDescription("Returns the daily topic for the specified ID.")
            .Produces<DailyTopicContent>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // POST /dailyTopics
            group.MapPost("/dailyTopics", async Task<Results<
                    Ok<string>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
            (
                DailyTopSectionSlot dto,
                [FromServices] IV2ContentDailyService service,
                HttpContext httpContext,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.CreateDailyTopicAsync(dto, cancellationToken);
                    return TypedResults.Ok(result);
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
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("CreateDailyTopic")
            .WithTags("DailyTopics")
            .WithSummary("Create a new Daily Topic")
            .WithDescription("Creates a new daily topic entry.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // GET /dailyTopics/{topicId}/slots
            group.MapGet("/dailyTopics/{topicId:guid}/slots", async Task<Results<
                    Ok<List<DailyTopSectionSlot>>,
                    ProblemHttpResult>>
            (
                Guid topicId,
                [FromServices] IV2ContentDailyService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var slots = await service.GetAllDailySlotsAsync(topicId, cancellationToken);
                    return TypedResults.Ok(slots);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Error retrieving slots", ex.Message);
                }
            })
            .WithName("GetDailySlots")
            .WithTags("DailyTopics")
            .WithSummary("Get all slots for a Daily Topic")
            .WithDescription("Returns up to 9 slots for the specified daily topic.")
            .Produces<List<DailyTopSectionSlot>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
       
            group.MapPost("/dailytopic", async Task<Results<
      Ok<string>,
      ForbidHttpResult,
      BadRequest<ProblemDetails>,
      ProblemHttpResult>>
  (
      DailyTopic topic,
      [FromServices]IV2ContentDailyService service,
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
  .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
             .RequireAuthorization();

            group.MapPost("/dailytopicById", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                DailyTopic topic,
                [FromServices]IV2ContentDailyService service,
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

            // PUT /dailyTopics/{topicId}/slots/{slot}
            group.MapPut("/dailyTopics/{topicId:guid}/slots/{slot:int}", async Task<Results<
                    Ok<string>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
            (
                Guid topicId,
                int slot,
                DailyTopSectionSlot dto,
                [FromServices] IV2ContentDailyService service,
                HttpContext httpContext,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var claimJson = httpContext.User
                        .Claims.FirstOrDefault(c => c.Type == "user")?.Value
                        ?? throw new InvalidDataException("User claim missing");
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

                    var uid = JsonSerializer
                        .Deserialize<JsonElement>(claimJson)
                        .GetProperty("uid")
                        .GetString()!;

                    var result = await service.UpsertDailySlotAsync(uid, topicId, slot, dto, cancellationToken);
                    return TypedResults.Ok(result);
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
                        Title = "Invalid Slot Content",
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
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("UpsertDailySlot")
            .WithTags("DailyTopics")
            .WithSummary("Create or update a Daily Topic slot")
            .WithDescription("Upserts one of the 9 fixed slots (1=TopStory, 2=Event, 3–9=Articles).")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
