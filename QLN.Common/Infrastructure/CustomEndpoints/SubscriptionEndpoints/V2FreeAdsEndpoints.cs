using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;

namespace QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints
{
    public static class V2FreeAdsEndpoints
    {
        public static RouteGroupBuilder MapV2FreeAdsEndpoints(this RouteGroupBuilder group)
        {
            group.MapV2PurchaseFreeAdsSubscription();
            group.MapV2ValidateFreeAdsUsage();
            group.MapV2RecordFreeAdsUsage();
            group.MapV2GetFreeAdsUsageSummary();
            group.MapV2GetUserFreeSubscriptions();
            group.MapV2GetRemainingFreeAdsQuota();

            return group;
        }

        public static RouteGroupBuilder MapV2PurchaseFreeAdsSubscription(this RouteGroupBuilder group)
        {
            group.MapPost("/v2/free-ads/purchase", async Task<Results<
                Ok<V2PurchaseResponseDto>,
                UnauthorizedHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                HttpContext httpContext,
                V2SubscriptionPurchaseRequestDto request,
                IV2SubscriptionService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    // Extract userId from JWT claim
                    var(uid,userName) = UserTokenHelper.ExtractUserAsync(httpContext);

                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    // Set the userId from the token
                    request.UserId = uid;

                    var subscriptionId = await service.PurchaseFreeAdsSubscriptionAsync(request, cancellationToken);

                    return TypedResults.Ok(new V2PurchaseResponseDto
                    {
                        Id = subscriptionId,
                        ProductCode = request.ProductCode,
                        Message = "FREE ads subscription created successfully.",
                        Success = true,
                        PurchasedAt = DateTime.UtcNow
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid FREE Product",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "FREE Ads Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("PurchaseFreeAdsSubscription")
            .WithTags("V2 FREE Ads")
            .WithSummary("Purchase a FREE ads subscription")
            .WithDescription("Creates a FREE ads subscription with category-based quotas.")
            .Produces<V2PurchaseResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }

        public static RouteGroupBuilder MapV2ValidateFreeAdsUsage(this RouteGroupBuilder group)
        {
            group.MapPost("/v2/free-ads/validate-usage", async Task<IResult> (
                [FromBody] FreeAdsValidationRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var isValid = await service.ValidateFreeAdsUsageAsync(
                        request.SubscriptionId,
                        request.Category,
                        request.L1Category,
                        request.L2Category,
                        request.RequestedAmount,
                        cancellationToken);

                    var remainingQuota = await service.GetRemainingFreeAdsQuotaAsync(
                        request.SubscriptionId,
                        request.Category,
                        request.L1Category,
                        request.L2Category,
                        cancellationToken);

                    var categoryPath = BuildCategoryPath(request.Category, request.L1Category, request.L2Category);

                    return Results.Ok(new FreeAdsValidationResponse
                    {
                        IsValid = isValid,
                        Message = isValid ? "FREE ads quota available" : "FREE ads quota exceeded",
                        SubscriptionId = request.SubscriptionId,
                        RequestedAmount = request.RequestedAmount,
                        CategoryPath = categoryPath,
                        RemainingQuota = remainingQuota,
                        Version = "V2"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "FREE Ads Validation Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("ValidateFreeAdsUsage")
            .WithTags("V2 FREE Ads")
            .WithSummary("Validate FREE ads usage")
            .WithDescription("Validates if a FREE ads subscription has enough quota for the requested category.")
            .Produces<FreeAdsValidationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }

        public static RouteGroupBuilder MapV2RecordFreeAdsUsage(this RouteGroupBuilder group)
        {
            group.MapPost("/v2/free-ads/record-usage", async Task<IResult> (
                [FromBody] FreeAdsRecordRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.RecordFreeAdsUsageAsync(
                        request.SubscriptionId,
                        request.Category,
                        request.L1Category,
                        request.L2Category,
                        request.Amount,
                        cancellationToken);

                    var remainingQuota = await service.GetRemainingFreeAdsQuotaAsync(
                        request.SubscriptionId,
                        request.Category,
                        request.L1Category,
                        request.L2Category,
                        cancellationToken);

                    var categoryPath = BuildCategoryPath(request.Category, request.L1Category, request.L2Category);

                    return Results.Ok(new FreeAdsRecordResponse
                    {
                        Success = result,
                        Message = result ? "FREE ads usage recorded successfully" : "Failed to record FREE ads usage",
                        SubscriptionId = request.SubscriptionId,
                        AmountRecorded = request.Amount,
                        CategoryPath = categoryPath,
                        RemainingQuota = remainingQuota,
                        RecordedAt = DateTime.UtcNow,
                        Version = "V2"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "FREE Ads Recording Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("RecordFreeAdsUsage")
            .WithTags("V2 FREE Ads")
            .WithSummary("Record FREE ads usage")
            .WithDescription("Records usage against a FREE ads subscription quota for specific category.")
            .Produces<FreeAdsRecordResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }

        public static RouteGroupBuilder MapV2GetFreeAdsUsageSummary(this RouteGroupBuilder group)
        {
            group.MapGet("/v2/free-ads/{subscriptionId}/usage-summary", async Task<IResult> (
                Guid subscriptionId,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var summary = await service.GetFreeAdsUsageSummaryAsync(subscriptionId, cancellationToken);

                    return Results.Ok(new
                    {
                        SubscriptionId = subscriptionId,
                        CategorySummaries = summary,
                        TotalCategories = summary.Count,
                        TotalFreeAdsUsed = summary.Sum(s => s.FreeAdsUsed),
                        TotalFreeAdsAllowed = summary.Sum(s => s.FreeAdsAllowed),
                        TotalFreeAdsRemaining = summary.Sum(s => s.FreeAdsRemaining),
                        Version = "V2"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "FREE Ads Usage Summary Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetFreeAdsUsageSummary")
            .WithTags("V2 FREE Ads")
            .WithSummary("Get FREE ads usage summary")
            .WithDescription("Retrieves usage summary for all categories in a FREE ads subscription.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }

        public static RouteGroupBuilder MapV2GetUserFreeSubscriptions(this RouteGroupBuilder group)
        {
            group.MapGet("/v2/free-ads/user", async Task<IResult> (
                HttpContext httpContext,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var (uid, userName) = UserTokenHelper.ExtractUserAsync(httpContext);

                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var subscriptions = await service.GetUserFreeSubscriptionsAsync(uid, cancellationToken);
                    return TypedResults.Ok(subscriptions);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                       title: "FREE Ads Usage Summary Error",
                       detail: ex.Message,
                       statusCode: StatusCodes.Status500InternalServerError
                   );
                }
            })
            .WithName("GetUserFreeSubscriptions")
            .WithTags("V2 FREE Ads")
            .WithSummary("Get user's FREE ads subscriptions")
            .WithDescription("Retrieves all active FREE ads subscriptions for the authenticated user.")
            .Produces<List<V2SubscriptionResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }

        public static RouteGroupBuilder MapV2GetRemainingFreeAdsQuota(this RouteGroupBuilder group)
        {
            group.MapGet("/v2/free-ads/remaining-quota", async Task<IResult> (
                [FromQuery] string category,
                [FromQuery] string? l1Category,
                [FromQuery] string? l2Category,
                [FromServices] IV2SubscriptionService service,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var (uid, userName) = UserTokenHelper.ExtractUserAsync(httpContext);
                    var remainingQuota = await service.GetRemainingFreeAdsQuotaForUserAsync(
                        uid, category, l1Category, l2Category, cancellationToken);

                    var categoryPath = BuildCategoryPath(category, l1Category, l2Category);

                    return Results.Ok(new
                    {
                        UserId = uid,
                        CategoryPath = categoryPath,
                        RemainingQuota = remainingQuota,
                        Version = "V2"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "FREE Ads Remaining Quota Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetRemainingFreeAdsQuota")
            .WithTags("V2 FREE Ads")
            .WithSummary("Get remaining FREE ads quota")
            .WithDescription("Gets remaining FREE ads quota for a specific category.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            return group;
        }

        private static string BuildCategoryPath(string category, string? l1Category, string? l2Category)
        {
            if (!string.IsNullOrEmpty(l2Category))
                return $"{category} > {l1Category} > {l2Category}";
            if (!string.IsNullOrEmpty(l1Category))
                return $"{category} > {l1Category}";
            return category;
        }
    }
}
