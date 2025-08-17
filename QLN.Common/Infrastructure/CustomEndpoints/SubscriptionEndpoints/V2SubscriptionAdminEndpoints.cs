using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.IService.IProductService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints
{
    public static class V2SubscriptionAdminEndpoints
    {
        public static RouteGroupBuilder MapV2AdminEndpoints(this RouteGroupBuilder group)
        {
            // Admin Subscription endpoints
            group.MapV2AdminUpdateSubscriptionStatus();
            group.MapV2AdminUpdateSubscriptionEndDate();
            group.MapV2AdminExtendSubscription();
            group.MapV2AdminRefillSubscriptionQuota();
            group.MapV2AdminCancelSubscription();
            group.MapV2AdminGetSubscriptionById();

            // Admin Addon endpoints
            group.MapV2AdminUpdateAddonStatus();
            group.MapV2AdminUpdateAddonEndDate();
            group.MapV2AdminExtendAddon();
            group.MapV2AdminRefillAddonQuota();
            group.MapV2AdminCancelAddon();
            group.MapV2AdminGetAddonById();

            return group;
        }

        public static RouteGroupBuilder MapV2AdminUpdateSubscriptionStatus(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/subscription/{subscriptionId}/status", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid subscriptionId,
                [FromBody] V2UpdateStatusRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.UpdateSubscriptionStatusAsync(subscriptionId, request.Status, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Subscription with ID {subscriptionId} not found.");
                    }

                    return TypedResults.Ok($"V2 Subscription status updated to {request.Status} successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Update Status Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminUpdateV2SubscriptionStatus")
            .WithTags("V2 Admin - Subscription")
            .WithSummary("Update V2 subscription status (Admin)")
            .WithDescription("Updates the status of a V2 subscription in both database and actor state.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminUpdateSubscriptionEndDate(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/subscription/{subscriptionId}/end-date", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid subscriptionId,
                [FromBody] V2UpdateEndDateRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (request.EndDate <= DateTime.UtcNow)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid End Date",
                            Detail = "End date must be in the future.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.UpdateSubscriptionEndDateAsync(subscriptionId, request.EndDate, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Subscription with ID {subscriptionId} not found.");
                    }

                    return TypedResults.Ok($"V2 Subscription end date updated to {request.EndDate:yyyy-MM-dd HH:mm:ss} UTC successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Update End Date Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminUpdateV2SubscriptionEndDate")
            .WithTags("V2 Admin - Subscription")
            .WithSummary("Update V2 subscription end date (Admin)")
            .WithDescription("Updates the end date of a V2 subscription in both database and actor state.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminExtendSubscription(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/subscription/{subscriptionId}/extend", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid subscriptionId,
                [FromBody] V2ExtendRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (request.AdditionalDays <= 0)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Duration",
                            Detail = "Additional days must be greater than 0.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var additionalDuration = TimeSpan.FromDays(request.AdditionalDays);
                    var result = await service.ExtendSubscriptionAsync(subscriptionId, additionalDuration, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Subscription with ID {subscriptionId} not found.");
                    }

                    return TypedResults.Ok($"V2 Subscription extended by {request.AdditionalDays} days successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Extend Subscription Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminExtendV2Subscription")
            .WithTags("V2 Admin - Subscription")
            .WithSummary("Extend V2 subscription (Admin)")
            .WithDescription("Extends a V2 subscription by specified number of days.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminRefillSubscriptionQuota(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/subscription/{subscriptionId}/refill-quota", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid subscriptionId,
                [FromBody] V2RefillQuotaRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (request.Amount <= 0)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Amount",
                            Detail = "Refill amount must be greater than 0.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.RefillSubscriptionQuotaAsync(subscriptionId, request.QuotaType, request.Amount, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Subscription with ID {subscriptionId} not found.");
                    }

                    return TypedResults.Ok($"V2 Subscription quota '{request.QuotaType}' refilled by {request.Amount} successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Refill Quota Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminRefillV2SubscriptionQuota")
            .WithTags("V2 Admin - Subscription")
            .WithSummary("Refill V2 subscription quota (Admin)")
            .WithDescription("Refills a specific quota type for a V2 subscription.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminCancelSubscription(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/subscription/{subscriptionId}/cancel", async Task<Results<
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
                    var result = await service.AdminCancelSubscriptionAsync(subscriptionId, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Subscription with ID {subscriptionId} not found.");
                    }

                    return TypedResults.Ok($"V2 Subscription {subscriptionId} cancelled by admin successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Cancel Subscription Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminCancelV2Subscription")
            .WithTags("V2 Admin - Subscription")
            .WithSummary("Cancel V2 subscription (Admin)")
            .WithDescription("Cancels a V2 subscription (admin override).")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminGetSubscriptionById(this RouteGroupBuilder group)
        {
            group.MapGet("/v2/admin/subscription/{subscriptionId}", async Task<Results<
                Ok<V2SubscriptionResponseDto>,
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
                    var subscription = await service.GetSubscriptionByIdAsync(subscriptionId, cancellationToken);

                    if (subscription == null)
                    {
                        return TypedResults.NotFound($"V2 Subscription with ID {subscriptionId} not found.");
                    }

                    return TypedResults.Ok(subscription);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Get Subscription Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminGetV2SubscriptionById")
            .WithTags("V2 Admin - Subscription")
            .WithSummary("Get V2 subscription by ID (Admin)")
            .WithDescription("Retrieves a V2 subscription by ID with full details.")
            .Produces<V2SubscriptionResponseDto>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }


        #region Admin Addon Endpoints

        public static RouteGroupBuilder MapV2AdminUpdateAddonStatus(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/addon/{addonId}/status", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid addonId,
                [FromBody] V2UpdateStatusRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.UpdateAddonStatusAsync(addonId, request.Status, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Addon with ID {addonId} not found.");
                    }

                    return TypedResults.Ok($"V2 Addon status updated to {request.Status} successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Update Addon Status Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminUpdateV2AddonStatus")
            .WithTags("V2 Admin - Addon")
            .WithSummary("Update V2 addon status (Admin)")
            .WithDescription("Updates the status of a V2 addon in both database and actor state.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminUpdateAddonEndDate(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/addon/{addonId}/end-date", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid addonId,
                [FromBody] V2UpdateEndDateRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (request.EndDate <= DateTime.UtcNow)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid End Date",
                            Detail = "End date must be in the future.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.UpdateAddonEndDateAsync(addonId, request.EndDate, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Addon with ID {addonId} not found.");
                    }

                    return TypedResults.Ok($"V2 Addon end date updated to {request.EndDate:yyyy-MM-dd HH:mm:ss} UTC successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Update Addon End Date Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminUpdateV2AddonEndDate")
            .WithTags("V2 Admin - Addon")
            .WithSummary("Update V2 addon end date (Admin)")
            .WithDescription("Updates the end date of a V2 addon in both database and actor state.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminExtendAddon(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/addon/{addonId}/extend", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid addonId,
                [FromBody] V2ExtendRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (request.AdditionalDays <= 0)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Duration",
                            Detail = "Additional days must be greater than 0.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var additionalDuration = TimeSpan.FromDays(request.AdditionalDays);
                    var result = await service.ExtendAddonAsync(addonId, additionalDuration, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Addon with ID {addonId} not found.");
                    }

                    return TypedResults.Ok($"V2 Addon extended by {request.AdditionalDays} days successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Extend Addon Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminExtendV2Addon")
            .WithTags("V2 Admin - Addon")
            .WithSummary("Extend V2 addon (Admin)")
            .WithDescription("Extends a V2 addon by specified number of days.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminRefillAddonQuota(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/addon/{addonId}/refill-quota", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid addonId,
                [FromBody] V2RefillQuotaRequest request,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (request.Amount <= 0)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Amount",
                            Detail = "Refill amount must be greater than 0.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.RefillAddonQuotaAsync(addonId, request.QuotaType, request.Amount, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Addon with ID {addonId} not found.");
                    }

                    return TypedResults.Ok($"V2 Addon quota '{request.QuotaType}' refilled by {request.Amount} successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Refill Addon Quota Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminRefillV2AddonQuota")
            .WithTags("V2 Admin - Addon")
            .WithSummary("Refill V2 addon quota (Admin)")
            .WithDescription("Refills a specific quota type for a V2 addon.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminCancelAddon(this RouteGroupBuilder group)
        {
            group.MapPut("/v2/admin/addon/{addonId}/cancel", async Task<Results<
                Ok<string>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid addonId,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.AdminCancelAddonAsync(addonId, cancellationToken);

                    if (!result)
                    {
                        return TypedResults.NotFound($"V2 Addon with ID {addonId} not found.");
                    }

                    return TypedResults.Ok($"V2 Addon {addonId} cancelled by admin successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Cancel Addon Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminCancelV2Addon")
            .WithTags("V2 Admin - Addon")
            .WithSummary("Cancel V2 addon (Admin)")
            .WithDescription("Cancels a V2 addon (admin override).")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        public static RouteGroupBuilder MapV2AdminGetAddonById(this RouteGroupBuilder group)
        {
            group.MapGet("/v2/admin/addon/{addonId}", async Task<Results<
                Ok<V2UserAddonResponseDto>,
                NotFound<string>,
                ProblemHttpResult>>
            (
                Guid addonId,
                [FromServices] IV2SubscriptionService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var addon = await service.GetAddonByIdAsync(addonId, cancellationToken);

                    if (addon == null)
                    {
                        return TypedResults.NotFound($"V2 Addon with ID {addonId} not found.");
                    }

                    return TypedResults.Ok(addon);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "V2 Admin Get Addon Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AdminGetV2AddonById")
            .WithTags("V2 Admin - Addon")
            .WithSummary("Get V2 addon by ID (Admin)")
            .WithDescription("Retrieves a V2 addon by ID with full details.")
            .Produces<V2UserAddonResponseDto>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            //.RequireAuthorization("AdminPolicy");

            return group;
        }

        #endregion
    }
}