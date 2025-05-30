using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToPublishService;
using Microsoft.AspNetCore.Builder;
using QLN.Common.DTOs;
using System.Security.Claims;

namespace QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints
{
    public static class PaymentEndpoint
    {
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
