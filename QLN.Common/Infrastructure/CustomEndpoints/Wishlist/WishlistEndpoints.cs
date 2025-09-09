using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.IService;
using System;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.Wishlist
{
    public static class WishlistEndpoints
    {
        public static RouteGroupBuilder MapWishlist(this RouteGroupBuilder group)
        {
            group.MapGet("/{vertical}/getWishlist", async Task<Results<Ok<List<Guid>>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, ProblemHttpResult>> (
                    string vertical,
                    IWishlistService svc,
                    HttpContext ctx
                ) =>
            {
                var userClaim = ctx.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (string.IsNullOrEmpty(userClaim))
                {
                    return TypedResults.Unauthorized();
                }

                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var userId = userData.GetProperty("uid").GetString();

                if (userId == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = ctx.Request.Path
                    });
                }

                try
                {
                    var favs = await svc.GetFavoritesAsync(userId, vertical);
                    return TypedResults.Ok(favs);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: ctx.Request.Path
                    );
                }
            })
            .WithName("GetWishlist")
            .WithTags("Wishlist")
            .WithSummary("Retrieve user wishlist")
            .WithDescription("Fetches the user's wishlist items for a given vertical.")
            .RequireAuthorization()
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/{vertical}/postWishlist/{itemId:guid}", async Task<Results<Ok, BadRequest<ProblemDetails>, UnauthorizedHttpResult, ProblemHttpResult>> (
                    string vertical,
                    Guid itemId,
                    IWishlistService svc,
                    HttpContext ctx
                ) =>
            {
                var userClaim = ctx.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (string.IsNullOrEmpty(userClaim))
                {
                    return TypedResults.Unauthorized();
                }

                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var userId = userData.GetProperty("uid").GetString();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = ctx.Request.Path
                    });
                }

                if (itemId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Item ID must be provided.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = ctx.Request.Path
                    });
                }

                try
                {
                    await svc.AddFavoriteAsync(userId, vertical, itemId);
                    return TypedResults.Ok();
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: ctx.Request.Path
                    );
                }
            })
            .WithName("PostWishlist")
            .WithTags("Wishlist")
            .WithSummary("Add an item to the wishlist")
            .WithDescription("Adds a specific item to the user's wishlist for a given vertical.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/{vertical}/deleteWishlist/{itemId:guid}", async Task<Results<NoContent, BadRequest<ProblemDetails>, UnauthorizedHttpResult, ProblemHttpResult>> (
                    string vertical,
                    Guid itemId,
                    IWishlistService svc,
                    HttpContext ctx
                ) =>
            {
                var userClaim = ctx.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (string.IsNullOrEmpty(userClaim))
                {
                    return TypedResults.Unauthorized();
                }

                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var userId = userData.GetProperty("uid").GetString();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = ctx.Request.Path
                    });
                }

                if (itemId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Item ID must be provided.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = ctx.Request.Path
                    });
                }

                try
                {
                    await svc.RemoveFavoriteAsync(userId, vertical, itemId);
                    return TypedResults.NoContent();
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: ctx.Request.Path
                    );
                }
            })
            .WithName("DeleteWishlist")
            .WithTags("Wishlist")
            .WithSummary("Remove an item from the wishlist")
            .WithDescription("Removes a specific item from the user's wishlist for a given vertical.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
