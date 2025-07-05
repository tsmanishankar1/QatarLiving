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

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2DailyEndpoint
    {
        public static RouteGroupBuilder MapDailyTopicEndpoints(this RouteGroupBuilder group)
        {
            // POST /dailyTopics
            group.MapPost(
                       "/dailySlots",
                       async Task<Results<
                           Created<string>,
                           BadRequest<ProblemDetails>,
                           ProblemHttpResult>>
                       (
                           [FromBody] DailyTopSectionSlot dto,
                           [FromServices] IV2ContentDailyService service,
                           HttpContext httpContext,
                           CancellationToken cancellationToken
                       ) =>
                       {

                           try
                           {
                               var result = await service.UpsertSlotAsync(dto, cancellationToken);

                               return TypedResults.Created(
                                   $"/dailySlots", result);
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
                       }
                   )
                   .WithName("UpsertDailySlot")
                   .WithTags("DailySlots")
                   .WithSummary("Create or update one of the 9 fixed daily slots")
                   .Produces<string>(StatusCodes.Status201Created)
                   .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                   .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // GET /dailySlots
            group.MapGet(
                "/dailySlots",
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

            return group;
        }
    }
}
