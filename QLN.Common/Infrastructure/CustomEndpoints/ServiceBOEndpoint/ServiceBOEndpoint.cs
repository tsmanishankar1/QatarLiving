using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using QLN.Common.Infrastructure.Subscriptions;
namespace QLN.Common.Infrastructure.CustomEndpoints.ServiceBOEndpoint
{
    public static class ServiceBOEndpoint
    {
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
        public static RouteGroupBuilder MapGetCompaniesByVertical(this RouteGroupBuilder group)
        {
            group.MapGet("/getByVertical", async Task<IResult> (
                [FromQuery] VerticalType verticalId,
                [FromQuery] SubVertical? subVerticalId,
                [FromServices] IServicesBoService service) =>
            {
                try
                {
                    var result = await service.GetCompaniesByVerticalAsync(verticalId, subVerticalId);

                    if (result == null || result.Count == 0)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "No companies found for the specified vertical and subvertical.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred while retrieving company profiles.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetCompaniesByVertical")
            .WithTags("ServicesBo")
            .WithSummary("Get company profiles by vertical and subvertical")
            .WithDescription("Retrieves company profiles based on the provided verticalId and optional subVerticalId.")
            .Produces<List<CompanyProfileDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }




    }
}
