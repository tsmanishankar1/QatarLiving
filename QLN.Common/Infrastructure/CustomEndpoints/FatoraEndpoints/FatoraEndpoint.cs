using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.IService.IPayToFeatureService;
using QLN.Common.Infrastructure.Subscriptions;
using System.Net;
using System.Text.Json;

namespace QLN.Common.Infrastructure.CustomEndpoints.FatoraEndpoints;

public static class FatoraEndpoints
{
    public static RouteGroupBuilder MapFatoraSuccessEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/fatora/webhooks/success", async Task<IResult> (
            [FromServices] IPaymentService service,
            [FromQuery(Name = "transaction_id")] string transactionId,
            [FromQuery(Name = "order_id")] string orderId,
            [FromQuery(Name = "card_token")] string? cardToken,
            [FromQuery(Name = "mode")] string? mode,
            [FromQuery(Name = "response_code")] string? responseCode,
            [FromQuery(Name = "description")] string? description,
            [FromQuery(Name = "platform")] string? platform,
            [FromQuery(Name = "vertical")] Vertical? vertical,
            [FromQuery(Name = "subscription_category")] SubscriptionCategory? subscriptionCategory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var request = new PaymentTransactionRequest
                {
                    TransactionId = transactionId,
                    OrderId = orderId,
                    CardToken = cardToken,
                    Mode = mode,
                    ResponseCode = responseCode,
                    Description = description,
                    Platform = platform,
                    Vertical = vertical,
                    SubscriptionCategory = subscriptionCategory,
                };

                var result = await service.PaymentSuccessAsync(request, cancellationToken);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
        })
            .WithName("FaturaSuccess")
            .WithTags("Payment")
            .Produces<string>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Handle Fatura Payment Success")
            .WithDescription("This endpoint handles the success of a Fatura payment.");




        return group;
    }

    public static RouteGroupBuilder MapFatoraFailureEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/fatora/webhooks/failure", async Task<IResult> (
            [FromServices] IPaymentService service,
            [FromQuery(Name = "transaction_id")] string transactionId,
            [FromQuery(Name = "order_id")] string orderId,
            [FromQuery(Name = "card_token")] string? cardToken,
            [FromQuery(Name = "mode")] string? mode,
            [FromQuery(Name = "response_code")] string? responseCode,
            [FromQuery(Name = "description")] string? description,
            [FromQuery(Name = "platform")] string? platform,
            [FromQuery(Name = "vertical")] Vertical? vertical,
            [FromQuery(Name = "subscription_category")] SubscriptionCategory? subscriptionCategory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var request = new PaymentTransactionRequest
                {
                    TransactionId = transactionId,
                    OrderId = orderId,
                    CardToken = cardToken,
                    Mode = mode,
                    ResponseCode = responseCode,
                    Description = description,
                    Platform = platform,
                    Vertical = vertical,
                    SubscriptionCategory = subscriptionCategory
                };

                var result = await service.PaymentFailureAsync(request, cancellationToken);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
        })
            .WithName("FaturaFailure")
            .WithTags("Payment")
            .Produces<string>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Handle Fatura Payment Failure")
            .WithDescription("This endpoint handles the failure of a Fatura payment.");

        return group;
    }
}