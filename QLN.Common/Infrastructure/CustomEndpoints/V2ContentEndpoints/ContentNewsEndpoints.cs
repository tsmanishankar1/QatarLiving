using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Utilities;

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
                [FromBody] ContentNewsDto dto,
                [FromServices] IV2ContentNews newsService,
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
                [FromBody] ContentNewsRequestByIdDto dto,
                [FromServices] IV2ContentNews newsService,
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

        public static RouteGroupBuilder MapContentBannerEndpoints(this RouteGroupBuilder group)
        {
            //  POST - Create Banner
            group.MapPost("/content/banner", async Task<Results<Ok<BannerResponse>, BadRequest<ProblemDetails>, ProblemHttpResult>>
            (
                [FromBody] BannerCreateRequest dto,
                [FromServices] IV2contentBannerService bannerService,
                HttpContext context,
                CancellationToken ct
            ) =>
            {
                var userId = context.User.GetId().ToString();

                if (string.IsNullOrWhiteSpace(userId))
                    return TypedResults.BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Authenticated user ID is missing." });
                if (dto == null)
                    return TypedResults.BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "BannerCreateRequest cannot be null." });
                if (string.IsNullOrWhiteSpace(dto.Category))
                    return TypedResults.BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Category is required." });
                if (string.IsNullOrWhiteSpace(dto.Code))
                    return TypedResults.BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Code is required." });

                dto.CreatedBy = userId;

                try
                {
                    var result = await bannerService.SaveBannerAsync(dto, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(title: "Failed to create banner", detail: ex.Message);
                }
            })
            .RequireAuthorization()
            .WithTags("Content Banner")
            .WithSummary("Create a new banner")
            .WithDescription("Creates a banner entry with blob images and stores in Dapr state store.")
            .Produces<BannerResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // GET by Category
            group.MapGet("/content/banner/{category}", async Task<Results<Ok<List<BannerItem>>, ProblemHttpResult>>
            (
                string category,
                [FromServices] IV2contentBannerService bannerService,
                HttpContext context,
                CancellationToken ct
            ) =>
            {
                if (string.IsNullOrWhiteSpace(category))
                    return TypedResults.Problem(title: "Validation Error", detail: "Category is required.");

                try
                {
                    var banners = await bannerService.GetBannersByCategoryAsync(category, ct);
                    return TypedResults.Ok(banners);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(title: "Failed to get banners", detail: ex.Message);
                }
            })
            .WithTags("Content Banner")
            .WithSummary("Get banners by category")
            .WithDescription("Returns banners for a given category from Dapr state store.")
            .Produces<List<BannerItem>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            //  PUT - Update Banner
            group.MapPut("/content/banner/update/{category}/{code}", async Task<Results<Ok<BannerResponse>, BadRequest<ProblemDetails>, ProblemHttpResult>>
            (
                string category,
                string code,
                [FromBody] BannerUpdateRequest dto,
                [FromServices] IV2contentBannerService bannerService,
                HttpContext context,
                CancellationToken ct
            ) =>
            {
                var userId = context.User.GetId().ToString();
                if (string.IsNullOrWhiteSpace(userId))
                    return TypedResults.BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Authenticated user ID is missing." });

                dto.Category = category;
                dto.Code = code;
                dto.UpdatedBy = userId;

                try
                {
                    var result = await bannerService.UpdateBannerAsync(dto, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(title: "Update Failed", detail: ex.Message);
                }
            })
            .RequireAuthorization()
            .WithTags("Content Banner")
            .WithSummary("Update banner by category/code")
            .WithDescription("Updates the banner with given category and code.")
            .Produces<BannerResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            //  DELETE - Remove Banner
            group.MapDelete("/content/banner/delete-state/{category}/{code}", async Task<Results<Ok, NotFound, ProblemHttpResult>>
              (
                  string category,
                  string code,
                  [FromServices] IV2contentBannerService bannerService,
                  CancellationToken ct
              ) =>
            {
                try
                {
                    var deleted = await bannerService.DeleteBannerFromStateAsync(category, code, ct);
                    return deleted ? TypedResults.Ok() : TypedResults.NotFound();
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Delete Failed",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
              .RequireAuthorization()
              .WithName("DeleteBannerFromState")
              .WithTags("Content Banner")
              .WithSummary("Internal API to delete banner directly from Dapr state store")
              .WithDescription("Removes a banner from the Dapr state store without external processing.");

            //  GET ALL - Grouped by QueueName
            group.MapGet("/content/banner/all", async Task<Results<Ok<Dictionary<string, BaseQueueResponse<BannerItem>>>, ProblemHttpResult>>
            (
                [FromServices] IV2contentBannerService bannerService,
                HttpContext context,
                CancellationToken ct
            ) =>
            {
                try
                {
                    var result = await bannerService.GetAllBannersAsync(ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(title: "Failed to load all banners", detail: ex.Message);
                }
            })
            .WithTags("Content Banner")
            .WithSummary("Get all banners grouped by queue")
            .WithDescription("Returns all banners grouped by queue name (e.g. qln_banners_daily_hero)")
            .Produces<Dictionary<string, BaseQueueResponse<BannerItem>>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}