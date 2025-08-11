using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using static QLN.Common.DTO_s.CommunityBo;
using System.Security.Claims;

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

                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(
                            title: "Unauthorized",
                            detail: "Authorization token is missing or invalid.",
                            statusCode: StatusCodes.Status401Unauthorized
                        );
                    }

                    JsonElement userData;
                    string uid;
                    try
                    {
                        userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                        uid = userData.GetProperty("uid").GetString();

                        if (string.IsNullOrEmpty(uid))
                        {
                            return TypedResults.Problem(
                                title: "Unauthorized",
                                detail: "User ID (uid) is missing in token.",
                                statusCode: StatusCodes.Status401Unauthorized
                            );
                        }

                        dto.UserName = userData.GetProperty("name").GetString();
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Invalid Token",
                            detail: $"Failed to parse user claim or extract required fields: {ex.Message}",
                            statusCode: StatusCodes.Status401Unauthorized
                        );
                    }

                    dto.UpdatedBy = uid;
                    dto.UpdatedDate = DateTime.UtcNow;
                    dto.IsActive = true;
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
   .WithName("CreateCommunityPost")
   .WithTags("V2Community")
   .WithSummary("Create Community Post")
   .WithDescription("Creates a community post and stores image in blob. Returns status message.")
   .Produces<string>(StatusCodes.Status200OK)
   .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
   .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
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
                 Ok<PaginatedCommunityPostResponseDto>,
                ProblemHttpResult
            >>
            (

                [FromQuery] string? categoryId,
                [FromQuery] string? search,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] string? sortDirection,
                IV2CommunityPostService service,
                CancellationToken ct
            ) =>
            {
                try
                {
                    var posts = await service.GetAllCommunityPostsAsync(categoryId, search, page, pageSize, sortDirection, ct);
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
            .Produces<PaginatedCommunityPostResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("getCommunityPostById/{id:guid}", async Task<IResult> (
                Guid id,
                CancellationToken ct,
                IV2CommunityPostService service) =>
            {
                try
                {
                    var post = await service.GetCommunityPostByIdAsync(id, ct);
                    if (post is null)
                        return Results.NotFound();

                    return Results.Ok(post);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
                .WithName("GetCommunityPostsById")
            .WithTags("V2Community")
            .WithSummary("Get by Id community posts")
            .WithDescription("Retrieves community posts with image URLs.")
            .Produces<PaginatedCommunityPostResponseDto>(StatusCodes.Status200OK)
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
                    return TypedResults.Ok("Community post deleted successfully.");
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

            group.MapPost("/likePostByCategoryId", async Task<Results<
                Ok<object>,
                BadRequest<ProblemDetails>,
                ForbidHttpResult,
                ProblemHttpResult>>
                (
                CommunityPostLikeDto dto,
                IV2CommunityPostService service, HttpContext httpContext,
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
                    dto.UserId = userData.GetProperty("uid").GetString();
                    dto.LikePostId = Guid.NewGuid();
                    dto.LikedDate = DateTime.UtcNow;

                    var liked = await service.LikePostForUser(dto, ct);
                    return TypedResults.Ok((object)new
                    {
                        status = liked ? "liked" : "unliked",
                        dto.CommunityPostId,
                        dto.UserId
                    });
                }
                catch (JsonException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid User Claim",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to process like operation.", ex.Message);
                }
            })
                .WithName("LikeCommunityPost")
                .WithTags("V2Community")
                .WithSummary("Like/Unlike a Community Post")
                .WithDescription("Toggles like for a community post based on user ID. Returns current like status.")
                .Produces<object>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/likePost", async Task<Results<
                Ok<object>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                CommunityPostLikeDto dto,
                IV2CommunityPostService service,
                CancellationToken ct
                ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.UserId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    dto.LikePostId = Guid.NewGuid();
                    dto.LikedDate = DateTime.UtcNow;

                    var liked = await service.LikePostForUser(dto, ct);

                    return TypedResults.Ok((object)new
                    {
                        status = liked ? "liked" : "unliked",
                        dto.CommunityPostId,
                        dto.UserId
                    });

                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
.WithName("LikeCommunityPostInternal")
                .WithTags("V2Community")
                .ExcludeFromDescription();

            group.MapPost("/addCommentByCategoryId", async Task<Results<
        Ok<object>,
        BadRequest<ProblemDetails>,
        ProblemHttpResult>>
    (
        CommunityCommentDto dto,
        IV2CommunityPostService service,
        HttpContext httpContext,
        CancellationToken ct
    ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(
                            title: "Unauthorized",
                            detail: "Authorization token is missing or invalid.",
                            statusCode: StatusCodes.Status401Unauthorized
                        );
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    dto.UserId = userData.GetProperty("uid").GetString();
                    dto.UserName = userData.GetProperty("name").GetString();
                    dto.CommentedAt = DateTime.UtcNow;
                    dto.CommentId = Guid.NewGuid();

                    await service.AddCommentToCommunityPostAsync(dto, ct);

                    return TypedResults.Ok((object)new
                    {
                        message = "Comment added successfully",
                        dto.CommunityPostId,
                        dto.UserId
                    });

                }
                catch (JsonException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid User Claim",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Failed to add comment.",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
    .WithName("AddCommentToCommunityPost")
    .WithTags("V2Community")
    .WithSummary("Add a comment to a Community Post")
    .WithDescription("Adds a new comment to a community post based on user token and CommunityPostId.")
    .Produces<object>(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/addComment", async Task<Results<
    Ok<object>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    CommunityCommentDto dto,
    IV2CommunityPostService service,
    CancellationToken ct
) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.UserId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    dto.CommentId = Guid.NewGuid();
                    dto.CommentedAt = DateTime.UtcNow;

                    await service.AddCommentToCommunityPostAsync(dto, ct);

                    return TypedResults.Ok((object)new
                    {
                        message = "Comment added successfully",
                        dto.CommunityPostId,
                        dto.UserId
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .WithName("AddCommentToCommunityPostInternal")
                .WithTags("V2Community")
                .ExcludeFromDescription();

            group.MapGet("/getCommentsByPostId/{postId:guid}", async Task<IResult> (
     Guid postId,
     int? page,
     int? perPage,
     IV2CommunityPostService service,
     HttpContext httpContext,
     CancellationToken ct) =>
            {
                string? userId = null;
                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    userId = httpContext.User.FindFirst("uid")?.Value
                             ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        var userClaim = httpContext.User.FindFirst("user")?.Value;
                        if (!string.IsNullOrWhiteSpace(userClaim))
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(userClaim);
                                if (doc.RootElement.TryGetProperty("uid", out var uidProp) &&
                                    uidProp.ValueKind == JsonValueKind.String)
                                {
                                    var uid = uidProp.GetString();
                                    if (!string.IsNullOrWhiteSpace(uid))
                                        userId = uid;
                                }
                            }
                            catch (JsonException)
                            {
                                return TypedResults.Problem("Jao");
                            }
                        }
                    }
                }

                var comments = await service.GetAllCommentsByPostIdAsync(postId, userId, page, perPage, ct);
                return Results.Ok(comments);
            })
 .WithName("GetCommunityPostComments")
 .WithTags("V2Community")
 .WithSummary("Get all comments for a community post")
 .WithDescription("Retrieves a paginated list of comments (with replies) by community post ID.")
 .Produces<CommunityCommentListResponse>(StatusCodes.Status200OK)
 .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/getCommentsByPost/{postId:guid}", async Task<IResult> (
Guid postId,
string? userId,
int? page,
int? perPage,
IV2CommunityPostService service,
CancellationToken ct) =>
            {
                var comments = await service.GetAllCommentsByPostIdAsync(postId, userId, page, perPage, ct);
                return Results.Ok(comments);
            })
                .ExcludeFromDescription()
.WithName("GetCommunityPostBy")
.WithTags("V2Community")
.WithSummary("Get all comments for a community post")
.WithDescription("Retrieves a paginated list of comments (with replies) by community post ID.")
.Produces<CommunityCommentListResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);


            group.MapPost("/likeCommentByUserId", async Task<Results<
                Ok<object>,
                ForbidHttpResult,
                ProblemHttpResult>>
            (
                LikeCommentsDto comment,
                IV2CommunityPostService service,
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

                    var liked = await service.LikeCommentAsync(comment, userId, ct);

                    return TypedResults.Ok((object)new
                    {
                        status = liked ? "liked" : "unliked",
                        comment.CommentId,
                        userId
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to process comment like operation.", ex.Message);
                }
            })
            .WithName("LikeCommentByUser")
            .WithTags("V2Community")
            .WithSummary("Like/Unlike a comment using JWT")
            .WithDescription("Extracts user ID from token and toggles comment like.");


            group.MapPost("/likeCommentInternal", async Task<Results<
    Ok<object>,
    ProblemHttpResult>>
(
   LikeCommentsDto comment,
    string userId,
    IV2CommunityPostService service,
    CancellationToken ct
) =>
            {
                try
                {
                    var liked = await service.LikeCommentAsync(comment, userId, ct);

                    return TypedResults.Ok((object)new
                    {
                        status = liked ? "liked" : "unliked",
                        comment.CommentId,
                        userId
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("LikeCommentInternal")
            .WithTags("V2Community")
            .ExcludeFromDescription();

            group.MapGet("/getBySlug/{slug}", async Task<Results<
            Ok<V2CommunityPostDto>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>>
        (
            string slug,
            IV2CommunityPostService service,
            CancellationToken cancellationToken
        ) =>
            {
                try
                {
                    var post = await service.GetCommunityPostBySlugAsync(slug, cancellationToken);
                    if (post is null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No community post found with slug: {slug}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(post);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
        .WithName("GetCommunityPostBySlug")
        .WithTags("V2Community")
        .WithSummary("Get Community Post by Slug")
        .WithDescription("Returns the community post for the provided slug.")
        .Produces<V2CommunityPostDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

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

            group.MapPost("/comments/delete/{postId}/{commentId}", async Task<Results<
          Ok<CommunityCommentApiResponse>,
     ForbidHttpResult,
     ProblemHttpResult>>
 (
     Guid postId,
     Guid commentId,
     IV2CommunityPostService service,
     HttpContext httpContext,
     CancellationToken ct
 ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrWhiteSpace(userClaim))
                        return TypedResults.Forbid();

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var userId = userData.GetProperty("uid").GetString();
                    if (string.IsNullOrWhiteSpace(userId))
                        return TypedResults.Forbid();

                    var result = await service.SoftDeleteCommunityCommentAsync(postId, commentId, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to delete community comment using JWT.", ex.Message);
                }
            })
 .WithName("SoftDeleteCommunityCommentJWT")
 .WithTags("V2Community")
 .WithSummary("Soft delete a community comment using JWT")
 .WithDescription("Sets IsActive=false for a community comment. Only the owner can delete their own comment.")
 .Produces<CommunityCommentApiResponse>(StatusCodes.Status200OK)
 .Produces(StatusCodes.Status403Forbidden)
 .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/comments/delete/byid/{postId}/{commentId}", async Task<Results<
     Ok<CommunityCommentApiResponse>,
     BadRequest<ProblemDetails>,
     ProblemHttpResult>>
 (
     Guid postId,
     Guid commentId,
     [FromQuery] string userId,
     IV2CommunityPostService service,
     CancellationToken ct
 ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Missing User ID",
                            Detail = "The 'userId' query parameter is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.SoftDeleteCommunityCommentAsync(postId, commentId, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to delete community comment with provided user ID.", ex.Message);
                }
            })
 .ExcludeFromDescription()
 .WithName("SoftDeleteCommunityCommentByUserId")
 .WithTags("Community")
 .WithSummary("Soft delete a community comment by ID (explicit userId)")
 .WithDescription("Used for admin/debug cases to delete a community comment by supplying the userId directly.")
 .Produces<CommunityCommentApiResponse>(StatusCodes.Status200OK)
 .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
 .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/comments/edit/{postId}/{commentId}", async Task<Results<
                Ok<CommunityCommentApiResponse>,
                ForbidHttpResult,
                ProblemHttpResult>>
            (
                Guid postId,
                Guid commentId,
                [FromBody] string updatedText,
                IV2CommunityPostService service,
                HttpContext httpContext,
                CancellationToken ct
            ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                        return TypedResults.Forbid();

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var userId = userData.GetProperty("uid").GetString();
                    if (string.IsNullOrWhiteSpace(userId))
                        return TypedResults.Forbid();

                    var result = await service.EditCommunityCommentAsync(postId, commentId, userId, updatedText, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to edit community comment.", ex.Message);
                }
            })
            .WithName("EditCommunityCommentJWT")
            .WithTags("V2Community")
            .WithSummary("Edit a community comment (JWT-based)")
            .WithDescription("Allows a user to edit their community comment by reading user ID from JWT token.")
            .Produces<CommunityCommentApiResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/comments/edit/byid/{postId}/{commentId}", async Task<Results<
                Ok<CommunityCommentApiResponse>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                Guid postId,
                Guid commentId,
                [FromQuery] string userId,
                [FromBody] string updatedText,
                IV2CommunityPostService service,
                CancellationToken ct
            ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Missing User ID",
                            Detail = "The 'userId' query parameter is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.EditCommunityCommentAsync(postId, commentId, userId, updatedText, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to edit community comment (by user ID).", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("EditCommunityCommentByUserId")
            .WithTags("V2Community")
            .WithSummary("Edit community comment with explicit user ID")
            .WithDescription("Used when the client provides the user ID directly in the query.")
            .Produces<CommunityCommentApiResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
