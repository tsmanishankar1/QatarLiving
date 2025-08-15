using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.IService.IPayments;
using QLN.Common.Infrastructure.IService.IPayToFeatureService;
using QLN.Common.Infrastructure.Subscriptions;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Web;

namespace QLN.Common.Infrastructure.CustomEndpoints.FatoraEndpoints;

public static class FatoraEndpoints
{
    public static RouteGroupBuilder MapPaymentCheckout(this RouteGroupBuilder group)
    {
        group.MapPost("/checkout", async Task<IResult> (
            [FromServices] IPaymentService payments,
            [FromBody] ExternalPaymentRequest request,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (request is null)
                return Results.BadRequest("Request body is required.");

            var user = http.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            var userDetailsFromClaims = ExtractUserDetailsFromClaims(user);

            var userDetailsFromJwt = ExtractUserDetailsFromJwtToken(http);

            var userDetails = userDetailsFromClaims ?? userDetailsFromJwt;

            if (userDetails == null || string.IsNullOrEmpty(userDetails.UserId))
            {
                return Results.BadRequest(new
                {
                    message = "Could not extract user details.",
                    error = new
                    {
                        code = "401",
                        description = "Unable to extract user information from authentication token."
                    }
                });
            }

            request.User = userDetails;

            var res = await payments.PayAsync(request, ct);

            var isOk = string.Equals(res?.Status, "success", StringComparison.OrdinalIgnoreCase);
            var redirectUrl = res?.Result?.CheckOutUrl;

            if (!isOk || string.IsNullOrWhiteSpace(redirectUrl))
            {
                return Results.BadRequest(new
                {
                    message = "Failed to initialize checkout.",
                    error = new
                    {
                        code = res?.Error?.ErrorCode,
                        description = res?.Error?.Description ?? res?.Error?.Message
                    }
                });
            }

            return Results.Ok(new { paymentId = request.OrderId, redirectUrl });
        })
        .RequireAuthorization()
        .WithName("CreateCheckout")
        .WithTags("Payment")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithSummary("Start a payment checkout")
        .WithDescription("Creates Payment (Pending) and returns Fatora checkout URL.");

        return group;
    }
    public static RouteGroupBuilder MapFatoraSuccessEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/fatora/webhooks/success", async Task<IResult> (
            [FromServices] IPaymentService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var rawQuery = httpContext.Request.QueryString.ToString();
                var decodedQuery = HttpUtility.HtmlDecode(rawQuery);

                var queryParams = QueryHelpers.ParseQuery(decodedQuery);

                var transactionId = queryParams.TryGetValue("transaction_id", out var txId) ? txId.ToString() : string.Empty;
                var orderId = queryParams.TryGetValue("order_id", out var ordId) ? ordId.ToString() : string.Empty;
                var cardToken = queryParams.TryGetValue("card_token", out var cardTkn) ? cardTkn.ToString() : null;
                var mode = queryParams.TryGetValue("mode", out var md) ? md.ToString() : null;
                var responseCode = queryParams.TryGetValue("response_code", out var respCode) ? respCode.ToString() : null;
                var description = queryParams.TryGetValue("description", out var desc) ? desc.ToString() : null;
                var platform = queryParams.TryGetValue("platform", out var plat) ? plat.ToString() : null;
                var productCode = queryParams.TryGetValue("product_code", out var prodCode) ? prodCode.ToString() : null;

                Vertical? vertical = null;
                if (queryParams.TryGetValue("vertical", out var verticalValue) &&
                    Enum.TryParse<Vertical>(verticalValue, true, out var parsedVertical))
                {
                    vertical = parsedVertical;
                }

                SubVertical? subVertical = null;
                if (queryParams.TryGetValue("subvertical", out var subVerticalValue) &&
                    Enum.TryParse<SubVertical>(subVerticalValue, true, out var parsedSubVertical))
                {
                    subVertical = parsedSubVertical;
                }

                var request = new PaymentTransactionRequest
                {
                    TransactionId = transactionId,
                    OrderId = orderId,
                    CardToken = cardToken,
                    Mode = mode,
                    ResponseCode = responseCode,
                    Description = description,
                    Platform = platform,
                    Vertical = vertical ?? Vertical.Classifieds,
                    SubVertical = subVertical,
                    ProductCode = productCode
                };

                var redirectUrl = await service.PaymentSuccessAsync(request, cancellationToken);
                return Results.Redirect(redirectUrl, permanent: false);
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
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var rawQuery = httpContext.Request.QueryString.ToString();
                var decodedQuery = HttpUtility.HtmlDecode(rawQuery);

                var queryParams = QueryHelpers.ParseQuery(decodedQuery);

                var transactionId = queryParams.TryGetValue("transaction_id", out var txId) ? txId.ToString() : string.Empty;
                var orderId = queryParams.TryGetValue("order_id", out var ordId) ? ordId.ToString() : string.Empty;
                var cardToken = queryParams.TryGetValue("card_token", out var cardTkn) ? cardTkn.ToString() : null;
                var mode = queryParams.TryGetValue("mode", out var md) ? md.ToString() : null;
                var responseCode = queryParams.TryGetValue("response_code", out var respCode) ? respCode.ToString() : null;
                var description = queryParams.TryGetValue("description", out var desc) ? desc.ToString() : null;
                var platform = queryParams.TryGetValue("platform", out var plat) ? plat.ToString() : null;
                var productCode = queryParams.TryGetValue("product_code", out var prodCode) ? prodCode.ToString() : null;

                Vertical? vertical = null;
                if (queryParams.TryGetValue("vertical", out var verticalValue) &&
                    !string.IsNullOrEmpty(verticalValue) &&
                    Enum.TryParse<Vertical>(verticalValue, true, out var parsedVertical))
                {
                    vertical = parsedVertical;
                }

                SubVertical? subVertical = null;
                if (queryParams.TryGetValue("subvertical", out var subVerticalValue) &&
                    !string.IsNullOrEmpty(subVerticalValue) &&
                    Enum.TryParse<SubVertical>(subVerticalValue, true, out var parsedSubVertical))
                {
                    subVertical = parsedSubVertical;
                }

                if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(orderId))
                {
                    return Results.BadRequest("Missing required parameters: transaction_id or order_id");
                }

                var request = new PaymentTransactionRequest
                {
                    TransactionId = transactionId,
                    OrderId = orderId,
                    CardToken = cardToken,
                    Mode = mode,
                    ResponseCode = responseCode,
                    Description = description,
                    Platform = platform,
                    Vertical = vertical ?? Vertical.Classifieds, 
                    SubVertical = subVertical,
                    ProductCode = productCode
                };

                var redirectUrl = await service.PaymentFailureAsync(request, cancellationToken);
                return Results.Redirect(redirectUrl, permanent: false);
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
    static UserReqDto? ExtractUserDetailsFromClaims(ClaimsPrincipal user)
    {
        try
        {
            var userDetails = new UserReqDto
            {
                UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                UserId = user.FindFirst("ql:uid")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                Email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                Mobile = user.FindFirst("ql:phone")?.Value ?? string.Empty
            };

            if (!string.IsNullOrEmpty(userDetails.UserId))
            {
                return userDetails;
            }

            return null;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    // Helper method to extract user details directly from JWT token
    static UserReqDto? ExtractUserDetailsFromJwtToken(HttpContext httpContext)
    {
        try
        {
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            var userClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
            if (string.IsNullOrEmpty(userClaim))
            {
                return null;
            }

            var userJson = JsonDocument.Parse(userClaim);
            var root = userJson.RootElement;

            var userDetails = new UserReqDto
            {
                UserId = root.TryGetProperty("uid", out var uidProp) ? uidProp.GetString() ?? string.Empty : string.Empty,
                UserName = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty,
                Email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? string.Empty : string.Empty,
                Mobile = root.TryGetProperty("phone", out var phoneProp) ? phoneProp.GetString() ?? string.Empty : string.Empty
            };

            return userDetails;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}