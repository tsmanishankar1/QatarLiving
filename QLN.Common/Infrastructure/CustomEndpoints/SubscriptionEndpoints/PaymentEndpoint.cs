using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.IAddonService;
using QLN.Common.Infrastructure.IService.IPayToPublishService;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static QLN.Common.DTO_s.AddonDto;

namespace QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints
{
    public static class PaymentEndpoint
    {
        public static RouteGroupBuilder MapProcessPaymentEndpoint(this RouteGroupBuilder group)
        {
            // Subscription payment processing endpoint
            group.MapPost("/subscribe", async (
                PaymentTransactionRequestDto request,
                HttpContext context,
                [FromServices] IExternalSubscriptionService service,
                [FromServices] IActorProxyFactory actorProxyFactory,
                [FromServices] ILogger<IExternalSubscriptionService> logger,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                                        ?? context.User.FindFirst("sub")
                                        ?? context.User.FindFirst("userId");

                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var transactionId = await service.CreatePaymentAsync(request, userId, cancellationToken);

                    logger.LogInformation("User {UserId} assigned Subscriber role after payment", userId);

                    var actorId = new ActorId(transactionId.ToString());
                    var paymentActor = actorProxyFactory.CreateActorProxy<IPaymentTransactionActor>(actorId, nameof(IPaymentTransactionActor));

                    logger.LogInformation("Payment transaction {TransactionId} created and actor scheduled for user {UserId}",
                        transactionId, userId);

                    return Results.Ok(new
                    {
                        Message = "Payment done successfully and role updated.",
                        TransactionId = transactionId
                    });
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("already have an active subscription"))
                {
                    logger.LogWarning(ex, "User already has active subscription");
                    return Results.Conflict(new ProblemDetails
                    {
                        Title = "Active Subscription Exists",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (InvalidDataException ex)
                {
                    logger.LogWarning(ex, "Invalid payment data");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Payment Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogWarning(ex, "Unauthorized access attempt");
                    return Results.Unauthorized();
                }
                catch (KeyNotFoundException ex)
                {
                    logger.LogWarning(ex, "Subscription not found");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Subscription Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing payment");
                    return Results.Problem(
                        title: "Payment Processing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("ProcessPayment")
            .WithTags("Payment")
            .WithSummary("Process subscription payment")
            .WithDescription("Processes payment and creates a payment transaction with scheduled expiry.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Pub/Sub endpoint for subscription expiry - Updated to use CloudEvent
            group.MapPost("/subscription-expiry",
          [Topic("pubsub", "subscription-expiry")]
            async (CloudEvent<SubscriptionExpiryMessage> cloudEvent,
            [FromServices] IExternalSubscriptionService subscriptionService,
            [FromServices] ILogger<IExternalSubscriptionService> logger,
             HttpContext context) =>
            {
                var message = cloudEvent.Data;


                try
                {
                    logger.LogInformation("=== PUBSUB ENDPOINT === Received expiry CloudEvent from {IP} for user {UserId}, sub {SubId}, tx {TxId}",
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                message?.UserId, message?.SubscriptionId, message?.PaymentTransactionId);

                    if (message == null || message.UserId == Guid.Empty)
                    {
                        logger.LogWarning("Received a null or invalid SubscriptionExpiryMessage. Skipping processing.");
                        return Results.BadRequest(new
                        {
                            Status = "Error",
                            Message = "Invalid or empty message received"
                        });
                    }

                    logger.LogInformation("Message details: UserId={UserId}, SubscriptionId={SubscriptionId}, PaymentId={PaymentId}, ExpiryDate={ExpiryDate}",
                message.UserId, message.SubscriptionId, message.PaymentTransactionId, message.ExpiryDate);

                    await subscriptionService.HandleSubscriptionExpiryAsync(message);

                    logger.LogInformation("Successfully processed subscription expiry for user {UserId}", message.UserId);

                    return Results.Ok(new
                    {
                        Status = "Success",
                        UserId = message.UserId,
                        Message = "Subscription expiry processed successfully"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process subscription expiry CloudEvent for user {UserId}", message?.UserId);
                    return Results.Problem(
                title: "Subscription Expiry Processing Error",
                detail: "An internal error occurred while processing the subscription expiry message.",
                statusCode: StatusCodes.Status500InternalServerError
            );
                }
            })
            .WithName("HandleSubscriptionExpiry")
            .WithTags("Internal")
            .AllowAnonymous()
            .ExcludeFromDescription();

            // Pub/Sub health check endpoint
            group.MapGet("/health/pubsub", (
                [FromServices] ILogger<IExternalSubscriptionService> logger) =>
            {
                logger.LogInformation("=== PUBSUB HEALTH CHECK === Accessed at {Time}", DateTime.UtcNow);
                return Results.Ok(new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Service = "SubscriptionService",
                    PubSubTopic = "subscription-expiry"
                });
            })
            .WithName("PubSubHealthCheck")
            .WithTags("Health")
            .AllowAnonymous()
            .ExcludeFromDescription();


            return group;
        }
        public static RouteGroupBuilder MapProcessPaytoPublishPaymentEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/paytopublish", async (
                PaymentRequestDto request,
                HttpContext context,
                [FromServices] IPayToPublishService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                                       ?? context.User.FindFirst("sub")
                                       ?? context.User.FindFirst("userId");

                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }


                    var transactionId = await service.CreatePaymentsAsync(request, userId, cancellationToken);



                    return Results.Ok(new { Message = "Payment done successfully", TransactionId = transactionId });
                }
                catch (InvalidDataException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Payment Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Subscription Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Payment Processing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("PaytoPublishPayment")
            .WithTags("Payment")
            .WithSummary("Process PayToPublish payment")
            .WithDescription("Processes payment for a PayToPublish and creates a payment transaction record.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/yearly-subscription", async (
                HttpContext context,
                [FromServices] IExternalSubscriptionService service,
                [FromServices] ILogger<IExternalSubscriptionService> logger,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    // Extract userId from token
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                                       ?? context.User.FindFirst("sub")
                                       ?? context.User.FindFirst("userId");

                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        logger.LogWarning("Unauthorized access attempt - no valid user ID in token");
                        return Results.Unauthorized();
                    }

                    logger.LogInformation("Checking yearly subscription status for user {UserId}", userId);

                    var result = await service.CheckYearlySubscriptionAsync(userId, cancellationToken);

                    if (result == null)
                    {
                        return Results.NotFound(new
                        {
                            Message = "No subscription data found for user",
                            UserId = userId
                        });
                    }

                    logger.LogInformation("Yearly subscription check completed for user {UserId}. IsYearly: {IsYearly}",
                        userId, result.IsRewardsYearlySubscription);

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error checking yearly subscription");
                    return Results.Problem(
                        title: "Yearly Subscription Check Error",
                        detail: "An internal error occurred while checking yearly subscription status.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("CheckYearlySubscription")
            .WithTags("Payment")
            .WithSummary("Check if user has yearly subscription")
            .WithDescription("Checks if the authenticated user has an active yearly subscription and returns subscription details.")
            .Produces<YearlySubscriptionResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }

        public static RouteGroupBuilder MapProcessAddonPaymentEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/addon", async (
                PaymentAddonRequestDto request,
                HttpContext context,
                [FromServices] IAddonService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                                       ?? context.User.FindFirst("sub")
                                       ?? context.User.FindFirst("userId");

                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }


                    var transactionId = await service.CreateAddonPaymentsAsync(request, userId, cancellationToken);



                    return Results.Ok(new { Message = "Payment done successfully", TransactionId = transactionId });
                }
                catch (InvalidDataException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Payment Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Addon Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Payment Processing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AddonPayment")
            .WithTags("Payment")
            .WithSummary("Process Addon payment")
            .WithDescription("Processes payment for a Addon and creates a payment transaction record.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
    }
}
