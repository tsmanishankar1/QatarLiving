using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using static QLN.Common.DTO_s.CommunityBo;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2CommunityEndpoints
    {
        public static RouteGroupBuilder MapCommunityEndpoints(this RouteGroupBuilder group)
        {
            // External (calls external service, which Dapr invokes internal)
            group.MapPost("/createPost", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                V2CommunityPostDto dto,
                IV2CommunityPostService service,
                HttpContext httpContext,
                CancellationToken ct
            ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    dto.UserName = userData.GetProperty("name").GetString();
                    dto.UpdatedBy = uid;
                    dto.UpdatedDate = DateTime.UtcNow;
                    dto.DateCreated = DateTime.UtcNow;

                    var result = await service.CreateCommunityPostAsync(uid, dto, ct);
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
            .WithName("CreateCommunityPost")
            .WithTags("V2Community")
            .WithSummary("Create Community Post")
            .WithDescription("Creates a community post and stores image in blob. Returns status message.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Internal for Dapr invocation:
            group.MapPost("/createPostInternal", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                V2CommunityPostDto dto,
                IV2CommunityPostService service,
                CancellationToken ct
            ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.UpdatedBy))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserName and UpdatedBy are required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityPostAsync(dto.UpdatedBy, dto, ct);
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
            .ExcludeFromDescription()
            .WithName("CreateCommunityPostInternal")
            .WithTags("V2Community");

            group.MapGet("/getAllPosts", async Task<Results<
                Ok<List<V2CommunityPostDto>>,
                ProblemHttpResult
            >>
            (
                IV2CommunityPostService service,
                CancellationToken ct
            ) =>
            {
                try
                {
                    var posts = await service.GetAllCommunityPostsAsync(ct);
                    return TypedResults.Ok(posts);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllCommunityPosts")
            .WithTags("V2Community")
            .WithSummary("Get all community posts")
            .WithDescription("Retrieves all community posts with image URLs.")
            .Produces<List<V2CommunityPostDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // External endpoint (calls internal via Dapr)
            group.MapDelete("/deletePost/{id:guid}", async Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>> (
                Guid id,
                IV2CommunityPostService service,
                HttpContext httpContext,
                CancellationToken ct
            ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();

                    var result = await service.SoftDeleteCommunityPostAsync(id, uid, ct);
                    if (!result)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Community post with id {id} not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok("Community post soft-deleted (IsActive=false) successfully.");
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
            .WithName("DeleteCommunityPost")
            .WithTags("V2Community")
            .WithSummary("Soft delete community post")
            .WithDescription("Sets IsActive=false for the given community post id.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapDelete("/deletePostInternal/{id:guid}", async Task<Results<Ok<bool>, NotFound<ProblemDetails>, ProblemHttpResult>> (
    Guid id,
    [FromBody] string userId,
    IV2CommunityPostService service,
    CancellationToken ct
) =>
            {
                try
                {
                    var result = await service.SoftDeleteCommunityPostAsync(id, userId, ct);
                    if (!result)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Community post with id {id} not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(true);
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
            .ExcludeFromDescription()
            .WithName("DeleteCommunityPostInternal")
            .WithTags("V2Community");

            return group;
        }

        public static RouteGroupBuilder MapCategoryEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getAllForumCategories", static async Task<Results<Ok<ForumCategoryListDto>, ProblemHttpResult>> (
      IV2CommunityPostService service,
      CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var categories = await service.GetAllForumCategoriesAsync(cancellationToken);
                    return TypedResults.Ok(categories);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
        
            .WithName("GetAllCommunityCategories")
            .WithTags("V2Community")
            .WithSummary("Get all community catrgory value with id for dropdown")
            .WithDescription("Retrieves all community categories as list..")
            .Produces<ForumCategoryListDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            return group;
        }
    }
}
