using Dapr.Actors;
using Dapr.Actors.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.IPayToPublishService;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using System.Security.Claims;
using Dapr;

namespace QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints
{
    public static class PaymentEndpoint
    {
        public static RouteGroupBuilder MapProcessPaymentEndpoint(this RouteGroupBuilder group)
        {
            // Your existing payment endpoint
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
                    // Extract user ID from claims
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                                     ?? context.User.FindFirst("sub")
                                     ?? context.User.FindFirst("userId");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    // Step 1: Create payment transaction using the service (business logic)
                    var transactionId = await service.CreatePaymentAsync(request, userId, cancellationToken);

                    logger.LogInformation("User {UserId} assigned Subscriber role after payment", userId);

                    var actorId = new ActorId(transactionId.ToString());
                    var paymentActor = actorProxyFactory.CreateActorProxy<IPaymentTransactionActor>(
                        actorId,
                        "PaymentTransactionActor");

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
                    logger.LogWarning(ex, "User {UserId} already has active subscription",
                        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    return Results.Conflict(new ProblemDetails
                    {
                        Title = "Active Subscription Exists",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (InvalidDataException ex)
                {
                    logger.LogWarning(ex, "Invalid payment data for user {UserId}",
                        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
                    logger.LogWarning(ex, "Subscription not found for request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Subscription Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing payment for user {UserId}",
                        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    return Results.Problem(
                        title: "Payment Processing Error",
                        detail: "An error occurred while processing the payment",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("ProcessPayment")
            .WithTags("Payment")
            .WithSummary("Process subscription payment")
            .WithDescription("Processes payment for a subscription and creates a payment transaction record with scheduled expiry check.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // ENHANCED: Pub/sub subscription endpoint for handling subscription expiry
            group.MapPost("/subscription-expiry", [Topic("pubsub", "subscription-expiry")] async (
                SubscriptionExpiryMessage message,
                [FromServices] IExternalSubscriptionService subscriptionService,
                [FromServices] ILogger<IExternalSubscriptionService> logger,
                HttpContext context) =>
            {
                try
                {
                    // Enhanced logging with request details
                    logger.LogInformation("=== PUBSUB ENDPOINT === Received subscription expiry message from {RemoteIP} for user {UserId}, subscription {SubscriptionId}, payment {PaymentId}",
                        context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        message.UserId, message.SubscriptionId, message.PaymentTransactionId);

                    // Validate the message
                    if (message == null)
                    {
                        logger.LogError("=== PUBSUB ENDPOINT ERROR === Received null message");
                        return Results.BadRequest(new { Status = "Error", Message = "Null message received" });
                    }

                    if (message.UserId == Guid.Empty)
                    {
                        logger.LogError("=== PUBSUB ENDPOINT ERROR === Received message with empty UserId");
                        return Results.BadRequest(new { Status = "Error", Message = "Invalid UserId" });
                    }

                    // Log the full message for debugging
                    logger.LogInformation("=== PUBSUB ENDPOINT === Message details: UserId={UserId}, SubscriptionId={SubscriptionId}, PaymentId={PaymentId}, ExpiryDate={ExpiryDate}, ProcessedAt={ProcessedAt}",
                        message.UserId, message.SubscriptionId, message.PaymentTransactionId, message.ExpiryDate, message.ProcessedAt);

                    // Process the subscription expiry
                    await subscriptionService.HandleSubscriptionExpiryAsync(message);

                    logger.LogInformation("=== PUBSUB ENDPOINT SUCCESS === Successfully processed subscription expiry for user {UserId}", message.UserId);

                    return Results.Ok(new
                    {
                        Status = "Success",
                        UserId = message.UserId,
                        ProcessedAt = DateTime.UtcNow,
                        Message = "Subscription expiry processed successfully"
                    });
                }
                catch (ArgumentException ex)
                {
                    logger.LogError(ex, "=== PUBSUB ENDPOINT VALIDATION ERROR === Invalid argument in message for user {UserId}", message?.UserId);
                    return Results.BadRequest(new { Status = "ValidationError", Message = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "=== PUBSUB ENDPOINT OPERATION ERROR === Invalid operation for user {UserId}", message?.UserId);
                    return Results.BadRequest(new { Status = "OperationError", Message = ex.Message });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "=== PUBSUB ENDPOINT CRITICAL ERROR === Failed to process subscription expiry message for user {UserId}", message?.UserId);

                    // Return 500 to indicate failure - Dapr will retry based on your retry policy
                    return Results.Problem(
                        title: "Subscription Expiry Processing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("HandleSubscriptionExpiry")
            .WithTags("Internal")
            .ExcludeFromDescription(); // Hide from Swagger since this is internal

            // ADDITIONAL: Health check endpoint for pub/sub debugging
            group.MapGet("/health/pubsub", async (
                [FromServices] IExternalSubscriptionService subscriptionService,
                [FromServices] ILogger<IExternalSubscriptionService> logger) =>
            {
                try
                {
                    logger.LogInformation("=== PUBSUB HEALTH CHECK === Endpoint accessed");

                    return Results.Ok(new
                    {
                        Status = "Healthy",
                        Timestamp = DateTime.UtcNow,
                        Service = "SubscriptionService",
                        PubSubTopic = "subscription-expiry"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "=== PUBSUB HEALTH CHECK ERROR === Health check failed");
                    return Results.Problem("Health check failed");
                }
            })
            .WithName("PubSubHealthCheck")
            .WithTags("Health")
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
            .WithName("PayToPublishPayment")
            .WithTags("Payment")
            .WithSummary("Process PayToPublish payment")
            .WithDescription("Processes payment for a PayToPublish and creates a payment transaction record.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
