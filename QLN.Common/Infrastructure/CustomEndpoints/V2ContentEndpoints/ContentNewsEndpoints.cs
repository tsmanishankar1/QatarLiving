using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class ContentNewsEndpoints
    {
        // Extension method to register endpoint and return RouteGroupBuilder for chaining
        public static RouteGroupBuilder MapContentNewsEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/content/news/process", async Task<Results<
                Ok<NewsSummary>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                ContentNewsDto dto,
                IV2ContentNews newsService,
                HttpContext context
            ) =>
            {
                string userId = context.User?.Identity?.Name ?? ""; 

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (dto == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "ContentNewsDto cannot be null.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var summary = await newsService.ProcessNewsContentAsync(dto, userId);
                    return TypedResults.Ok(summary);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Processing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path
                    );
                }
            })
            .WithName("ProcessNewsContent")
            .WithTags("Content News")
            .WithSummary("Process submitted news content")
            .WithDescription("Processes ContentNewsDto by category and topic, and returns a summary.")
            .RequireAuthorization()
            .Produces<NewsSummary>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/content/news/process-by-id", async Task<Results<
                Ok<NewsSummary>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                ContentNewsRequestByIdDto dto,
                IV2ContentNews newsService,
                HttpContext context
            ) =>
            {
                if (string.IsNullOrWhiteSpace(dto?.UserId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (dto.NewsContent == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "ContentNewsDto cannot be null.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var summary = await newsService.ProcessNewsContentAsync(dto.NewsContent, dto.UserId);
                    return TypedResults.Ok(summary);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Processing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path
                    );
                }
            })
            .WithName("ProcessNewsContentById")
            .WithTags("Content News")
            .WithSummary("Process news content by explicit user ID")
            .WithDescription("Processes news content using an explicit user ID provided in the request.")
            .RequireAuthorization()
            .Produces<NewsSummary>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group; 
        }
    }
}
