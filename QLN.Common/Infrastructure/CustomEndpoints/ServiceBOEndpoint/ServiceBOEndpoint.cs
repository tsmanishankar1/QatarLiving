using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Auditlog;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System.Text.Json;
namespace QLN.Common.Infrastructure.CustomEndpoints.ServiceBOEndpoint
{
    public static class ServiceBOEndpoint
    {
        const string ModuleName = "Services";
        public static RouteGroupBuilder MapServiceAdGetAllEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getallbo", async (
                IServicesBoService service,
                CancellationToken cancellationToken,
                [FromQuery] string? sortBy = null,
                [FromQuery] string? search = null,
                [FromQuery] DateTime? fromDate = null,
                [FromQuery] DateTime? toDate = null,
                [FromQuery] DateTime? publishedFrom = null,
                [FromQuery] DateTime? publishedTo = null,
                [FromQuery] int? status = null,
                [FromQuery] bool? isPromoted = null,
                [FromQuery] bool? isFeatured = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 12) =>
            {
                try
                {
                    var result = await service.GetAllServiceBoAds(
                        sortBy ?? "CreationDate",
                        search,
                        fromDate,
                        toDate,
                        publishedFrom,
                        publishedTo,
                        status,
                        isFeatured,
                        isPromoted,
                        pageNumber,
                        pageSize,
                        cancellationToken
                    );
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, title: "Internal Server Error");
                }
            })
            .WithName("GetAllServiceAdsBo")
            .WithTags("ServicesBo")
            .WithSummary("Get all service ads with pagination")
            .WithDescription("Retrieves a paginated summary of all service ads with optional sorting, search, status, report, feature and date filters.")
            .Produces<PaginatedResult<ServiceAdSummaryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceAdPaymentSummaryEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getalladpayments", async (
                [AsParameters] PaginationQuery pagination,
                [FromQuery] string? search,
                [FromQuery] string? sortBy,
                [FromQuery] DateTime? startDate,
                [FromQuery] DateTime? endDate,
                [FromQuery] string? subscriptionType,
                IServicesBoService externalService,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("ServiceAdPaymentSummary");

                try
                {
                    var result = await externalService.GetAllServiceAdPaymentSummaries(
                        pagination.PageNumber ?? 1,
                        pagination.PageSize ?? 12,
                        search,
                        sortBy,
                        startDate,
                        endDate,
                        subscriptionType,
                        cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "❌ Error retrieving service ad payment summaries - InvalidOperation");
                    return Results.Problem(
                        detail: ex.Message,
                        title: "Data Processing Error",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Unexpected error occurred while retrieving service ad payment summaries.");
                    return Results.Problem(
                        detail: "An unexpected error occurred.",
                        title: "Internal Server Error",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetAllServiceSubscriptionListing")
            .WithTags("ServicesBo")
            .WithSummary("Get all service ads with Subscription info")
            .WithDescription("Returns all service ads with basic contact and payment details, with pagination, search, sorting, and filtering.")
            .Produces<PaginatedResult<ServiceAdPaymentSummaryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceP2PAdGetAllEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getallp2pbo", async (
                IServicesBoService service,
                CancellationToken cancellationToken,
                [FromQuery] string? sortBy = null,
                [FromQuery] string? search = null,
                [FromQuery] DateTime? fromDate = null,
                [FromQuery] DateTime? toDate = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 12) =>
            {
                try
                {
                    var result = await service.GetAllP2PServiceBoAds(
                        sortBy ?? "CreationDate",
                        search,
                        fromDate,
                        toDate,
                        pageNumber,
                        pageSize,
                        cancellationToken
                    );
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, title: "Internal Server Error");
                }
            })
            .WithName("GetAllP2PServiceAdsBo")
            .WithTags("ServicesBo")
            .WithSummary("Get all  P2P Transaction  ads with pagination")
            .WithDescription("Retrieves a paginated summary of all service ads with optional sorting, search, status, report, feature and date filters.")
            .Produces<PaginatedResult<ServiceP2PAdSummaryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceSubscriptionAdGetAllEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getallsubscriptionadsbo", async (
                IServicesBoService service,
                CancellationToken cancellationToken,
                [FromQuery] string? sortBy = null,
                [FromQuery] string? search = null,
                [FromQuery] DateTime? fromDate = null,
                [FromQuery] DateTime? toDate = null,
                [FromQuery] DateTime? publishedFrom = null,
                [FromQuery] DateTime? publishedTo = null,
                
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 12) =>
            {
                try
                {
                    var result = await service.GetAllSubscriptionAdsServiceBo(
                        sortBy ?? "CreationDate",
                        search,
                        fromDate,
                        toDate,
                        publishedFrom,
                        publishedTo,
                        pageNumber,
                        pageSize,
                        cancellationToken
                    );
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, title: "Internal Server Error");
                }
            })
            .WithName("GetAllSubscriptionsAdsBo")
            .WithTags("ServicesBo")
            .WithSummary("Get all service ads with pagination")
            .WithDescription("Retrieves a paginated summary of all SubscriptionsAds ads with optional sorting, search, status, report, feature and date filters.")
            .Produces<PaginatedResult<ServiceAdSummaryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapBulkActionEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/bulkaction", async Task<Results<
                    Ok<BulkAdActionResponseitems>,
                    ForbidHttpResult,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult
                >> (
                    BulkModerationRequest req,
                    HttpContext httpContext,
                    AuditLogger auditLogger,
                    IServicesBoService service,
                    CancellationToken ct
                ) =>
            {
                var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (string.IsNullOrEmpty(userClaim))
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unauthorized Access",
                        Detail = "User information is missing or invalid in the token.",
                        Status = StatusCodes.Status403Forbidden
                    });
                }
                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var uid = userData.GetProperty("uid").GetString();
                if (uid == null)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unauthorized Access",
                        Detail = "User ID could not be extracted from token.",
                        Status = StatusCodes.Status403Forbidden
                    });
                }
                req.UpdatedBy = uid;
                try
                {
                    var result = await service.ModerateBulkService(req, uid, ct);
                    await auditLogger.LogAuditAsync(
                        module: ModuleName,
                        httpMethod: "POST",
                        apiEndpoint: "/api/service/moderatebulk",
                        message: "Bulk moderation action performed successfully",
                        createdBy: uid,
                        payload: req,
                        cancellationToken: ct
                    );
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/moderatebulk", ex, uid, ct);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ConflictException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/moderatebulk", ex, uid, ct);
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Conflict",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/moderatebulk", ex, uid, ct);
                    return TypedResults.Problem(ex.Message);
                }
            })
                .WithName("BulkModerateBoServices")
                .WithTags("ServicesBo")
                .WithSummary("Bulk moderate service ads")
                .WithDescription("Performs bulk moderation actions (approve, publish, unpublish, remove) on selected service ads. " +
                                 "Requires a list of ad IDs and the action to perform. " +
                                 "If removing, a reason must be provided.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/bobulkactions", async Task<Results<
               Ok<BulkAdActionResponseitems>,
               BadRequest<ProblemDetails>,
               ForbidHttpResult,
               ProblemHttpResult
           >> (
               BulkModerationRequest req,
               string userId,
               HttpContext httpContext,
               IServicesBoService service,
               CancellationToken ct
           ) =>
            {
                try
                {
                    if (req.UpdatedBy == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "UpdatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.ModerateBulkService(req, userId, ct);
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
                catch (ConflictException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Conflict",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })
           .ExcludeFromDescription()
           .WithName("ModerateBoServices")
           .WithTags("ServicesBo")
           .WithSummary("Bulk moderate service ads")
           .WithDescription("Performs bulk moderation actions (approve, publish, unpublish, remove) on selected service ads. " +
                            "Requires a list of ad IDs and the action to perform. " +
                            "If removing, a reason must be provided.")
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
    }
}
