using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.IService.IPayToFeatureService;
using System.Net;
using System.Text.Json;

namespace QLN.Common.Infrastructure.CustomEndpoints.PayToFeatureEndpoint;

public static class FaturaEndpoint
{

    public static RouteGroupBuilder MapFaturaPaymentEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async Task<IResult> (
            [FromServices] IPaymentService service,
            [FromHeader] string? Platform,
            [FromBody] ExternalPaymentRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.PayAsync(request, cancellationToken);

                if(result.Status != "success")
                {
                    return TypedResults.Problem("Payment Failed", result.Error?.Description, StatusCodes.Status400BadRequest);
                }

                if (string.IsNullOrEmpty(result.Result?.CheckOutUrl))
                {
                    return TypedResults.Problem("Payment URL not found", "The payment URL is missing in the response.", StatusCodes.Status400BadRequest);
                }

                return TypedResults.Created(result.Result.CheckOutUrl, result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
        })
            .WithName("FaturaPayment")
            .WithTags("Payment")
            .Produces<PaymentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Process Fatura Payment")
            .WithDescription("This endpoint processes a Fatura payment.");
        return group;
    }

    public static RouteGroupBuilder MapFaturaSuccessEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/fatora/webhooks/success", async Task<IResult> (
            [FromServices] IPaymentService service,
            [FromBody] PaymentTransactionRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
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

    public static RouteGroupBuilder MapFaturaFailureEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/fatora/webhooks/failure", async Task<IResult> (
            [FromServices] IPaymentService service,
            [FromBody] PaymentTransactionRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
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