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

namespace QLN.Common.Infrastructure.CustomEndpoints.FatoraEndpoints;

public static class FatoraEndpoints
{
    public static RouteGroupBuilder MapFatoraSuccessEndpoint(this RouteGroupBuilder group)
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

    public static RouteGroupBuilder MapFatoraFailureEndpoint(this RouteGroupBuilder group)
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