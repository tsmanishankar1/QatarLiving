using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTOs;
using System.Security.Claims;


 
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
            [FromQuery] int categoryId,
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
    public static RouteGroupBuilder MapProcessPaymentEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/subscribe", async (
            PaymentTransactionRequestDto request,
            HttpContext context,
            [FromServices] IExternalSubscriptionService service,
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
        .WithName("ProcessPayment")
        .WithTags("Payment")
        .WithSummary("Process subscription payment")
        .WithDescription("Processes payment for a subscription and creates a payment transaction record.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }
  

}






