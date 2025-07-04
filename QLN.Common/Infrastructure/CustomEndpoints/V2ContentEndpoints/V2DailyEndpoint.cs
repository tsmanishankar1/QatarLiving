using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QLN.Common.Infrastructure.IService.IContentService;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2DailyEndpoint
    {
        public static RouteGroupBuilder MapCreateDailyEndpoints(this RouteGroupBuilder group)
        {
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
