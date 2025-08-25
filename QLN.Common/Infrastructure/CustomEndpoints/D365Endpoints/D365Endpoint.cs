using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IPayments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.D365Endpoints
{
    public static class D365Endpoint
    {
        public static RouteGroupBuilder MapD365PayEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/d365", async Task<IResult> (
                [FromServices] IOptions<D365Config> config,
                [FromServices] ID365Service service,
                [FromBody] D365Orders request,
                [FromHeader(Name = "x-api-key")] string XApiKey,
                CancellationToken cancellationToken) =>
            {
                if(string.IsNullOrEmpty(XApiKey))
                {
                    return TypedResults.BadRequest("API Key is required.");
                }

                if(XApiKey != config.Value.D365ApiKey)
                {
                    return TypedResults.Unauthorized();
                }

                try
                {
                    var results = await service.D365OrdersAsync(request.Orders, cancellationToken);

                    var resultString = results.Count > 1 ? string.Join(",", results) : results.First();

                    //return TypedResults.Ok(new { Message = resultString });

                    return TypedResults.Ok(new { Message = results });

                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("D365Payment")
                .WithTags("Payment")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<string>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithSummary("Handle D365 Payments")
                .WithDescription("This endpoint handles the D365 payment processing.");

            return group;
        }
    }
}
