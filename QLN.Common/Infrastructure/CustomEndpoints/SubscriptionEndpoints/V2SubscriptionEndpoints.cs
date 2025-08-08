using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.IService.IProductService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints
{
    public static class V2SubscriptionEndpoints
    {
        public static RouteGroupBuilder MapV2SubscriptionEndpoints(this RouteGroupBuilder group)
        {
            // Subscription endpoints
            group.MapV2PurchaseSubscription();
            group.MapV2GetSubscriptionsByVertical();
            group.MapV2GetUserSubscriptions();
            group.MapV2GetAllActiveSubscriptions();
            group.MapV2CancelSubscription();
            group.MapV2UsageValidation();
            group.MapV2UsageRecording();

            // Addon endpoints
            group.MapV2PurchaseAddon();
            group.MapV2AddonUsageValidation();
            group.MapV2AddonUsageRecording();
            group.MapV2GetUserAddons();

            return group;
        }

        #region Subscription Endpoints

        public static RouteGroupBuilder MapV2PurchaseSubscription(this RouteGroupBuilder group)
        {
            group.MapPost("/v2/purchase", async Task<Results<
                Ok<V2PurchaseResponseDto>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                V2SubscriptionPurchaseRequestDto request,
                IV2SubscriptionService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var subscriptionId = await service.PurchaseSubscriptionAsync(request, cancellationToken);

                    return TypedResults.Ok(new V2PurchaseResponseDto
                    {
                        Id = subscriptionId,
                        ProductCode = request.ProductCode,
                        Message = "V2 Subscription purchased successfully.",
                        Success = true,
                        PurchasedAt = DateTime.UtcNow
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Product",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("PurchaseV2Subscription")
            .WithTags("V2 Subscription")
            .WithSummary("Purchase a V2 subscription")
            .WithDescription("Purchases a V2 subscription based on product code.")
            .Produces<V2PurchaseResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapV2GetSubscriptionsByVertical(this RouteGroupBuilder group)
        {
            group.MapGet("/v2/vertical/{verticalTypeId}", async Task<IResult> (
                int verticalTypeId,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetSubscriptionsByVerticalAsync(verticalTypeId, cancellationToken);

                    if (result == null || !result.Subscriptions.Any())
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "V2 Subscriptions Not Found",
                            Detail = $"No active V2 subscriptions found for VerticalTypeId '{verticalTypeId}'",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetV2SubscriptionsByVertical")
            .WithTags("V2 Subscription")
            .WithSummary("Get V2 subscriptions by vertical")
            .WithDescription("Retrieves all active V2 subscriptions for a specific vertical.")
            .Produces<V2SubscriptionGroupResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapV2GetUserSubscriptions(this RouteGroupBuilder group)
        {
            group.MapGet("/v2/user/{userId}", async Task<IResult> (
                string userId,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var subscriptions = await service.GetUserSubscriptionsAsync(userId, cancellationToken);
                    return TypedResults.Ok(subscriptions);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetV2UserSubscriptions")
            .WithTags("V2 Subscription")
            .WithSummary("Get user's V2 subscriptions")
            .WithDescription("Retrieves all V2 subscriptions for a specific user.")
            .Produces<List<V2SubscriptionResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapV2GetAllActiveSubscriptions(this RouteGroupBuilder group)
        {
            group.MapGet("/v2/active", async Task<IResult> (
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var subscriptions = await service.GetAllActiveSubscriptionsAsync(cancellationToken);
                    return TypedResults.Ok(subscriptions);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetAllActiveV2Subscriptions")
            .WithTags("V2 Subscription")
            .WithSummary("Retrieve all active V2 subscriptions")
            .WithDescription("Fetches all active V2 subscriptions across all users")
            .Produces<List<V2SubscriptionResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapV2CancelSubscription(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/cancel/{subscriptionId}", async Task<Results<
                Ok<string>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid subscriptionId,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.CancelSubscriptionAsync(subscriptionId, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Subscription with ID {subscriptionId} not found.");
                    }

                    return TypedResults.Ok("V2 Subscription cancelled successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("CancelV2Subscription")
            .WithTags("V2 Subscription")
            .WithSummary("Cancel a V2 subscription")
            .WithDescription("Cancels an existing V2 subscription.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapV2UsageValidation(this RouteGroupBuilder group)
        {
            group.MapPost("/v2/validate-usage", async Task<IResult> (
                [FromBody] V2UsageValidationRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var isValid = await service.ValidateSubscriptionUsageAsync(
                        request.SubscriptionId,
                        request.QuotaType,
                        request.RequestedAmount,
                        cancellationToken);

                    return Results.Ok(new V2UsageValidationResponseDto
                    {
                        IsValid = isValid,
                        Message = isValid ? "V2 Usage validation successful" : "V2 Insufficient quota",
                        SubscriptionId = request.SubscriptionId,
                        QuotaType = request.QuotaType,
                        RequestedAmount = request.RequestedAmount,
                        AvailableQuota = 0, // TODO: Get from service
                        Version = "V2"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "V2 Validation Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("ValidateV2Usage")
            .WithTags("V2 Subscription")
            .WithSummary("Validate V2 subscription usage")
            .WithDescription("Validates if a V2 subscription has enough quota for the requested usage.")
            .Produces<V2UsageValidationResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapV2UsageRecording(this RouteGroupBuilder group)
        {
            group.MapPost("/v2/record-usage", async Task<IResult> (
                [FromBody] V2UsageRecordRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.RecordSubscriptionUsageAsync(
                        request.SubscriptionId,
                        request.QuotaType,
                        request.Amount,
                        cancellationToken);

                    return Results.Ok(new V2UsageRecordResponseDto
                    {
                        Success = result,
                        Message = result ? "V2 Usage recorded successfully" : "V2 Failed to record usage",
                        SubscriptionId = request.SubscriptionId,
                        QuotaType = request.QuotaType,
                        AmountRecorded = request.Amount,
                        RemainingQuota = 0, // TODO: Get from service
                        RecordedAt = DateTime.UtcNow,
                        Version = "V2"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "V2 Usage Recording Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("RecordV2Usage")
            .WithTags("V2 Subscription")
            .WithSummary("Record V2 subscription usage")
            .WithDescription("Records usage against a V2 subscription quota.")
            .Produces<V2UsageRecordResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        #endregion

        #region Addon Endpoints

        public static RouteGroupBuilder MapV2PurchaseAddon(this RouteGroupBuilder group)
        {
            group.MapPost("/v2/addon/purchase", async Task<Results<
                Ok<V2PurchaseResponseDto>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                V2UserAddonPurchaseRequestDto request,
                IV2SubscriptionService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var addonId = await service.PurchaseAddonAsync(request, cancellationToken);

                    return TypedResults.Ok(new V2PurchaseResponseDto
                    {
                        Id = addonId,
                        ProductCode = request.ProductCode,
                        Message = "V2 Addon purchased successfully.",
                        Success = true,
                        PurchasedAt = DateTime.UtcNow
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Addon Product",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Addon Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("PurchaseV2Addon")
            .WithTags("V2 Addon")
            .WithSummary("Purchase a V2 addon")
            .WithDescription("Purchases a V2 addon based on product code.")
            .Produces<V2PurchaseResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapV2AddonUsageValidation(this RouteGroupBuilder group)
        {
            group.MapPost("/v2/addon/validate-usage", async Task<IResult> (
                [FromBody] V2AddonUsageRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var isValid = await service.ValidateAddonUsageAsync(
                        request.AddonId,
                        request.QuotaType,
                        request.RequestedAmount,
                        cancellationToken);

                    return Results.Ok(new
                    {
                        IsValid = isValid,
                        Message = isValid ? "V2 Addon usage validation successful" : "V2 Addon insufficient quota",
                        AddonId = request.AddonId,
                        QuotaType = request.QuotaType,
                        RequestedAmount = request.RequestedAmount,
                        Version = "V2"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "V2 Addon Validation Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("ValidateV2AddonUsage")
            .WithTags("V2 Addon")
            .WithSummary("Validate V2 addon usage")
            .WithDescription("Validates if a V2 addon has enough quota for the requested usage.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapV2AddonUsageRecording(this RouteGroupBuilder group)
        {
            group.MapPost("/v2/addon/record-usage", async Task<IResult> (
                [FromBody] V2AddonUsageRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.RecordAddonUsageAsync(
                        request.AddonId,
                        request.QuotaType,
                        request.RequestedAmount,
                        cancellationToken);

                    return Results.Ok(new
                    {
                        Success = result,
                        Message = result ? "V2 Addon usage recorded successfully" : "V2 Failed to record addon usage",
                        AddonId = request.AddonId,
                        QuotaType = request.QuotaType,
                        Amount = request.RequestedAmount,
                        RecordedAt = DateTime.UtcNow,
                        Version = "V2"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "V2 Addon Usage Recording Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("RecordV2AddonUsage")
            .WithTags("V2 Addon")
            .WithSummary("Record V2 addon usage")
            .WithDescription("Records usage against a V2 addon quota.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapV2GetUserAddons(this RouteGroupBuilder group)
        {
            group.MapGet("/v2/addon/user/{userId}", async Task<IResult> (
                string userId,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var addons = await service.GetUserAddonsAsync(userId, cancellationToken);
                    return Results.Ok(addons);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "V2 User Addons Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetV2UserAddons")
            .WithTags("V2 Addon")
            .WithSummary("Get user V2 addons")
            .WithDescription("Retrieves all V2 addons for a specific user.")
            .Produces<List<V2UserAddonResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        #endregion
    }
}