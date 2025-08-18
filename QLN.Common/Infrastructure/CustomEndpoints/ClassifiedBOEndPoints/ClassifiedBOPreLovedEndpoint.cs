using Google.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Auditlog;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedBOEndPoints
{
    public static class ClassifiedBOPreLovedEndpoint
    {
        const string ModuleName = "Preloved";
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
          int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    int page = Page ?? 1;
                    int pageSize = PageSize ?? 12;
                    var result = await service.ViewPreLovedSubscriptions(subscriptionType, filterDate, page, pageSize, Search,SortBy,SortOrder, cancellationToken);

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
                .Produces<ClassifiedBOPageResponse<PrelovedViewSubscriptionsDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/preloved-p2p-subscriptions", async Task<Results<
          Ok<ClassifiedBOPageResponse<PreLovedViewP2PDto>>,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices] IClassifiedPreLovedBOService service,
          HttpContext context,
          string? Status,
          string? createdDate,
          string? publishedDate,
          int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    int page = Page ?? 1;
                    int pageSize = PageSize ?? 12;
                    var result = await service.ViewPreLovedP2PSubscriptions(Status,createdDate, publishedDate, page, pageSize, Search, SortBy, SortOrder, cancellationToken);

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
                .Produces<ClassifiedBOPageResponse<PreLovedViewP2PDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/preloved-p2p-transactions", async Task<Results<
          Ok<ClassifiedBOPageResponse<PreLovedViewP2PTransactionDto>>,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices] IClassifiedPreLovedBOService service,
          HttpContext context,
          string? createdDate,
          string? publishedDate,
          int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    int page = Page ?? 1;
                    int pageSize = PageSize ?? 12;
                    var result = await service.ViewPreLovedP2PTransactions(createdDate, publishedDate, page, pageSize, Search, SortBy, SortOrder, cancellationToken);

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
                .WithName("ViewPreLovedP2PTransactions")
                .AllowAnonymous()
                .WithTags("ClassifiedBo")
                .WithSummary("To list p2p transactions in the preloved.")
                .WithDescription("Fetches p2p transactions of users on preloved")
                .Produces<ClassifiedBOPageResponse<PreLovedViewP2PTransactionDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPut("/preloved-bulk-edit-subscriptions", async Task<Results<
          Ok<string>,
          ForbidHttpResult,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices] IClassifiedPreLovedBOService service,
          HttpContext httpContext,
          BulkEditPreLovedP2PDto dto,
          AuditLogger auditLogger,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    var (userId, validSubscriptions, error) = GenericClaimsHelper.GetValidSubscriptions(httpContext.User, (int)Vertical.Classifieds, (int)SubVertical.Stores);
                    if (!string.IsNullOrEmpty(error))
                    {
                        return TypedResults.Problem(
                        title: "Subscription issue in token.",
                        detail: error,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: httpContext.Request.Path
                        );
                    }

                    if (string.IsNullOrEmpty(userId))
                    {
                        return TypedResults.Forbid();
                    }
                    if (dto==null)
                        return TypedResults.BadRequest(new ProblemDetails() { Detail="Bulk PreLoved edit object cannot be null."});
                    else if(dto.AdIds == null || dto.AdStatus==0)
                        return TypedResults.BadRequest(new ProblemDetails() { Detail = "AdIs cannot be null or Status not be '0'." });
                    

                    if (validSubscriptions != null && validSubscriptions.Any())
                    {
                        var result = await service.BulkEditP2PSubscriptions(dto, userId, cancellationToken);
                        await auditLogger.LogAuditAsync(
                            module: ModuleName,
                            httpMethod: "POST",
                            apiEndpoint: "/api/v2/classifiedbo/preloved-bulk-edit-subscriptions",
                            message: "P2P status update successfully",
                            createdBy: userId,
                            payload: dto,
                            cancellationToken: cancellationToken
                        );

                        return TypedResults.Ok(result);
                    }
                    else
                    {
                        return TypedResults.Forbid();
                    }
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: httpContext.Request.Path
                    );
                }
            })
                .WithName("BulEditP2PSubscriptions")
                .WithTags("ClassifiedBo")
                .WithSummary("Edit subscriptions on preloved.")
                .WithDescription("Edit the status information of preloved subscriptions.")
                .Produces<BulkEditPreLovedP2PDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                 .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            
            group.MapPut("/preloved-bulk-edits-subscriptions/{userId}", async Task<Results<
          Ok<string>,
          ForbidHttpResult,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices] IClassifiedPreLovedBOService service,
          HttpContext httpContext,
          string? userId,
          BulkEditPreLovedP2PDto dto,
          AuditLogger auditLogger,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    

                    var result = await service.BulkEditP2PSubscriptions(dto,userId, cancellationToken);
                    
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: httpContext.Request.Path
                    );
                }
            })
                .ExcludeFromDescription()
                .WithName("BulEditsP2PSubscriptions")
                .WithTags("ClassifiedBo")
                .WithSummary("Edit subscriptions on preloved.")
                .WithDescription("Edit the status information of preloved subscriptions.")
                .Produces<BulkEditPreLovedP2PDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                 .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;

        }
    }
}
