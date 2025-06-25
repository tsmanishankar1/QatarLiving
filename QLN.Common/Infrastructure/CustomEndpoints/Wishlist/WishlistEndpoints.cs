using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.Wishlist
{
    public static class WishlistEndpoints
    {
        public static RouteGroupBuilder MapWishlist(this RouteGroupBuilder group)
        {
            group.MapGet("/{vertical}/getWishlist", async (
                    string vertical,
                    IWishlistService svc,
                    HttpContext ctx
                ) =>
            {
                var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var favs = await svc.GetFavoritesAsync(userId, vertical);
                return TypedResults.Ok(favs);
            })
            .RequireAuthorization();

            group.MapPost("/{vertical}/postWishlist/{itemId:guid}", async (
                    string vertical,
                    Guid itemId,
                    IWishlistService svc,
                    HttpContext ctx
                ) =>
            {
                var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                await svc.AddFavoriteAsync(userId, vertical, itemId);
                return TypedResults.Ok();
            })
            .RequireAuthorization();

            group.MapDelete("/{vertical}/deleteWishlist/{itemId:guid}", async (
                    string vertical,
                    Guid itemId,
                    IWishlistService svc,
                    HttpContext ctx
                ) =>
            {
                var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                await svc.RemoveFavoriteAsync(userId, vertical, itemId);
                return TypedResults.NoContent();
            })
            .RequireAuthorization();

            return group;
        }
    }
}
