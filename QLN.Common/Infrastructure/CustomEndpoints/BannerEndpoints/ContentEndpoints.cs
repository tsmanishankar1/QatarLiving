using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IBannerService;
using QLN.Common.Infrastructure.Model;
using System;
using System.Linq;

namespace QLN.Common.Infrastructure.CustomEndpoints.BannerEndpoints
{
    public static class BannerEndpoints
    {
        public static RouteGroupBuilder MapGetAllBannerEndpoint(this RouteGroupBuilder group)
        {

            // GET /api/Banner/landing
            group.MapGet("/", async (
                    [FromServices] IBannerService svc,
                    CancellationToken cancellationToken
                    )
                =>
            {
                try
                {
                    var model = await svc.GetBannersAsync(cancellationToken);

                    return Results.Ok(model);

                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Landing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetBanners")
            .WithTags("Banner");

            return group;
        }
    }
}

