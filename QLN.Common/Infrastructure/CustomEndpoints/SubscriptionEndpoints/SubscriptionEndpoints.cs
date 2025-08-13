using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using System.Security.Claims;
using System.Text.Json;



 
public static class SubscriptionEndpoints
{
    public static RouteGroupBuilder MapCreateSubscriptionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/add", async Task<Results<
            Ok<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            SubscriptionRequestDto request,
            IExternalSubscriptionService service,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                await service.CreateSubscriptionAsync(request, cancellationToken);

                return TypedResults.Ok("Subscription created successfully.");
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
            catch (Exception ex)
            {
                return TypedResults.Problem(
                    title: "Internal Server Error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        //.RequireAuthorization(policy => policy.RequireRole("Admin")) 
        .WithName("CreateSubscription")
        .WithTags("Subscription")
        .WithSummary("Create a new subscription")
        .WithDescription("Creates a new subscription with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapGetdetails(this RouteGroupBuilder group)
    {
        group.MapGet("/getsubscription", async Task<IResult> (
            [FromQuery] int verticalTypeId,
            [FromQuery] int? categoryId,
            [FromServices] IExternalSubscriptionService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.GetSubscriptionsByVerticalAndCategoryAsync(verticalTypeId, categoryId, cancellationToken);

                // Throw if empty to be caught below
                if (result == null || !result.Subscriptions.Any())
                    throw new KeyNotFoundException($"No subscriptions found for VerticalTypeId '{verticalTypeId}' and CategoryId '{categoryId}'.");

                return TypedResults.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (Exception ex)
            {
                return TypedResults.Problem(
                    title: "Internal Server Error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetSubscriptionByVerticalAndCategory")
        .WithTags("Subscription")
        .WithSummary("Get subscriptions by vertical and category")
        .WithDescription("Retrieves all subscriptions based on Vertical Type ID and Category ID.")
        .Produces<List<SubscriptionResponseDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }




    public static RouteGroupBuilder MapGetAllSubscription(this RouteGroupBuilder group)
    {
        group.MapGet("/getAll", async Task<IResult> (
            [FromServices] IExternalSubscriptionService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var subscriptions = await service.GetAllSubscriptionsAsync(cancellationToken);
                return TypedResults.Ok(subscriptions);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem(
                    title: "Internal Server Error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetAllSubscriptions")
        .WithTags("Subscription")
        .WithSummary("Retrieve all active subscriptions")
        .WithDescription("Fetches all subscriptions")
        .Produces<List<SubscriptionResponseDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }
    public static RouteGroupBuilder MapUpdateSubscription(this RouteGroupBuilder group)
    {
        group.MapPut("/update", async Task<Results<
            Ok<string>,
            NotFound<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            [FromHeader(Name = "Subscription-Id")] Guid subscriptionId,
            [FromBody] SubscriptionRequestDto request,
            [FromServices] IExternalSubscriptionService service,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
                var result = await service.UpdateSubscriptionAsync(subscriptionId, request, cancellationToken);

                if (!result)
                {
                    return TypedResults.NotFound($"Subscription with ID {subscriptionId} not found.");
                }

                return TypedResults.Ok("Subscription updated successfully.");
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
            catch (Exception ex)
            {
                return TypedResults.Problem(
                    title: "Internal Server Error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("UpdateSubscription")
        .WithTags("Subscription")
        .WithSummary("Update an existing subscription")
        .WithDescription("Updates an existing subscription using the Subscription-Id passed in the header.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }


    public static RouteGroupBuilder MapdeleteSubscription(this RouteGroupBuilder group)
    {
        group.MapDelete("/delete", async (
        [FromHeader(Name = "Subscription-Id")] Guid id,
        [FromServices] IExternalSubscriptionService service,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.DeleteSubscriptionAsync(id, cancellationToken);
            if (!result)
            {
                return Results.NotFound("Subscription  not found.");
            }

            return Results.Ok("Subscription  deleted successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(" Subscription not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            return Results.Problem("Internal server error: {ex.Message}");
        }
       })
        .WithName("DeleteSubscription")
        .WithTags("Subscription")
        .WithSummary("Delete an existing subscription")
        .WithDescription("Delete an existing subscription using the Subscription-Id passed in the header.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        return group;
    }
    public static RouteGroupBuilder MapGetUserPaymentDetailsEndpoint(this RouteGroupBuilder group)
    {
        // Get user payment details endpoint
        group.MapGet("/user-payments", async (
    HttpContext context,
    [FromServices] IExternalSubscriptionService service,
    [FromServices] ILogger<IExternalSubscriptionService> logger,
    CancellationToken cancellationToken) =>
        {
            try
            {
                // Extract UID from token using the new format
                var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                if (string.IsNullOrEmpty(userClaim))
                {
                    logger.LogWarning("User claim not found in token");
                    return Results.Unauthorized();
                }

                JsonElement userData;
                string uid;

                try
                {
                    userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse user claim or extract UID from token");
                    return Results.Unauthorized();
                }

                if (string.IsNullOrEmpty(uid))
                {
                    logger.LogWarning("Invalid or missing UID in user claim");
                    return Results.Unauthorized();
                }

                logger.LogInformation("Retrieving payment details for user {UserId}", uid);
                var paymentDetails = await service.GetUserPaymentDetailsAsync(uid, cancellationToken);

                if (paymentDetails == null || paymentDetails.Count == 0)
                {
                    logger.LogInformation("No payment details found for user {UserId}", uid);
                    return Results.Ok(paymentDetails);
                }

                return Results.Ok(paymentDetails);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "No payment data found.");
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Payment Details Not Found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving payment details for user");
                return Results.Problem(
                    title: "Payment Details Retrieval Error",
                    detail: "An internal error occurred while retrieving payment details.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetUserPaymentDetails")
        .WithTags("Payment")
        .WithSummary("Get user payment details")
        .WithDescription("Retrieves all payment transactions and corresponding subscription details for the authenticated user.")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapGet("/user-payments/{userId:guid}", async (
          string userId,
          [FromServices] IExternalSubscriptionService service,
          [FromServices] ILogger<IExternalSubscriptionService> logger,
          CancellationToken cancellationToken) =>
        {
            try
            {
                logger.LogInformation("Retrieving payment details for user {UserId}", userId);

                var paymentDetails = await service.GetUserPaymentDetailsAsync(userId, cancellationToken);

                if (paymentDetails == null || !paymentDetails.Any())
                {
                    logger.LogInformation("No payment details found for user {UserId}", userId);
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "No Payment Records Found",
                        Detail = $"No payment records exist for user with ID {userId}.",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                return Results.Ok(paymentDetails);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving payment details for user {UserId}", userId);
                return Results.Problem(
                    title: "Payment Retrieval Error",
                    detail: "An internal error occurred while retrieving payment records.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
      .WithName("GetAllUserPayments")
      .WithTags("Payment")
      .WithSummary("Get all payments by user ID")
      .WithDescription("Retrieves all payment transactions and subscription details for the specified user ID.")
      .Produces<object>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }
}






