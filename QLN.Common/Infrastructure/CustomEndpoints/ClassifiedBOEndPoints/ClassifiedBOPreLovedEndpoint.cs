using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s.ClassifiedsBo;

using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedBOEndPoints
{
    public static class ClassifiedBOPreLovedEndpoint
    {
        public static RouteGroupBuilder MapClassifiedBOPreLovedEndpoints(this RouteGroupBuilder group)
        {

            group.MapGet("/preloved-view-subscriptions", async Task<Results<
          Ok<ClassifiedBOPageResponse<PrelovedViewSubscriptionsDto>>,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices] IClassifiedPreLovedBOService service,
          HttpContext context,
          string? subscriptionType,
          string? filterDate,
          int? Page, int? PageSize, string? Search,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    int page = Page ?? 1;
                    int pageSize = PageSize ?? 12;
                    var result = await service.ViewPreLovedSubscriptions(subscriptionType, filterDate, page, pageSize, Search, cancellationToken);

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path
                    );
                }
            })
                .WithName("ViewPreLovedSubscriptions")
                .AllowAnonymous()
                .WithTags("ClassifiedBo")
                .WithSummary("To list all subscriptions in the preloved.")
                .WithDescription("Fetches all subscriptions of users on preloved")
                .Produces<ClassifiedBOPageResponse<StoresSubscriptionDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/preloved-p2p-subscriptions", async Task<Results<
          Ok<ClassifiedBOPageResponse<PreLovedViewP2PDto>>,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices] IClassifiedPreLovedBOService service,
          HttpContext context,
          string? createdDate,
          string? publishedDate,
          int? Page, int? PageSize, string? Search,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    int page = Page ?? 1;
                    int pageSize = PageSize ?? 12;
                    var result = await service.ViewPreLovedP2PSubscriptions(createdDate, publishedDate, page, pageSize, Search, cancellationToken);

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path
                    );
                }
            })
                .WithName("ViewPreLovedP2PSubscriptions")
                .AllowAnonymous()
                .WithTags("ClassifiedBo")
                .WithSummary("To list p2p subscriptions in the preloved.")
                .WithDescription("Fetches p2p subscriptions of users on preloved")
                .Produces<ClassifiedBOPageResponse<StoresSubscriptionDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;

        }
    }
}
