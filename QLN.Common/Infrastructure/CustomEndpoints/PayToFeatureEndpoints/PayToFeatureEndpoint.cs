using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToFeatureService;

using System.Security.Claims;

namespace QLN.Common.Infrastructure.CustomEndpoints.PayToFeatureEndpoint;

public static class PayToFeatureEndpoints
{
    public static RouteGroupBuilder MapCreatePayToFeatureEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/add", async Task<Results<
            Ok<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            PayToFeatureRequestDto request,
            IPayToFeatureService service,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                await service.CreatePlanAsync(request, cancellationToken);
                return TypedResults.Ok("PayToFeature plan created successfully.");
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
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("CreatePayToFeature")
        .WithTags("PayToFeature")
        .WithSummary("Create a new PayToFeature")
        .WithDescription("Creates a new PayToFeature with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapGetPayToFeatureEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/getpaytofeature", async Task<IResult> (
        [FromQuery] int verticalTypeId,
        [FromQuery] int categoryId,
        [FromServices] IPayToFeatureService service,
        CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.GetPlansByVerticalAndCategoryWithBasicPriceAsync(verticalTypeId, categoryId, cancellationToken);

                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetPayToFeature")
        .WithTags("PayToFeature")
        .WithSummary("Get PayToFeature plans by Vertical and Category")
        .WithDescription("Get PayToFeature plans filtered by the provided vertical type and category.")
        .Produces<PayToFeatureListResponseDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        return group;
    }

    public static RouteGroupBuilder MapGetAllPayToFeatureEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/getAll", async Task<IResult> (
            [FromServices] IPayToFeatureService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.GetAllPlansWithBasicPriceAsync(cancellationToken);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetAllPayToFeaturePlans")
        .WithTags("PayToFeature")
        .WithSummary("Get All PayToFeature Details")
        .WithDescription("Get all PayToFeature plans.")
        .Produces<List<PayToFeatureResponseDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapUpatePayToFeatureEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/update", async Task<Results<
            Ok<string>,
            NotFound<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            [FromHeader(Name = "PayToFeature-Id")] Guid planId,
            [FromBody] PayToFeatureRequestDto request,
            [FromServices] IPayToFeatureService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var updated = await service.UpdatePlanAsync(planId, request, cancellationToken);
                if (!updated)
                    return TypedResults.NotFound($"Plan with ID {planId} not found.");

                return TypedResults.Ok("PayToFeature plan updated successfully.");
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
                return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("UpdatePayToFeaturePlans")
        .WithTags("PayToFeature")
        .WithSummary("Update a PayToFeature")
        .WithDescription("Update a PayToFeature with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapDeletePayToFeatureEndpoints(this RouteGroupBuilder group)
    {
        group.MapDelete("/delete", async Task<IResult> (
            [FromHeader(Name = "PayToFeature-Id")] Guid planId,
            [FromServices] IPayToFeatureService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var deleted = await service.DeletePlanAsync(planId, cancellationToken);
                if (!deleted)
                    return Results.NotFound("PayToFeature plan not found.");

                return Results.Ok("PayToFeature plan deleted successfully.");
            }
            catch (Exception ex)
            {
                return Results.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("DeletePayToFeaturePlan")
        .WithTags("PayToFeature")
        .WithSummary("Delete a PayToFeature")
        .WithDescription("Deletes a PayToFeature with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapGetPayToFeaturePaymentsByUserEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/paytofeature/user", async (
            HttpContext context,
            [FromServices] IPayToFeatureService service,
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

                var result = await service.GetPaymentsByUserIdAsync(userId, cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error Retrieving Payments",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .RequireAuthorization()
        .WithName("GetPayToFeaturePaymentsByUser")
        .WithTags("PayToFeature")
        .WithSummary("Get PayToFeature payments by user")
        .Produces<List<PayToFeaturePaymentDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapGetPayToFeaturePaymentsByUserIdEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/paytofeature/user/{userId:guid}", async (
            Guid userId,
            [FromServices] IPayToFeatureService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.GetPaymentsByUserIdAsync(userId, cancellationToken);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Error Retrieving Payments",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetPayToFeaturePaymentsByUserId")
        .WithTags("PayToFeature")
        .WithSummary("Get PayToFeature payments by userId")
        .Produces<List<PayToFeaturePaymentDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapPayToFeatureCreateBasicPriceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/paytofeaturebasicprice/add", async Task<Results<
            Ok<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            PayToFeatureBasicPriceRequestDto request,
            IPayToFeatureService service,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                await service.CreateBasicPriceAsync(request, cancellationToken);
                return TypedResults.Ok("Basic price created successfully.");
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
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("CreatePayToFeatureBasicPrice")
        .WithTags("PayToFeature")
        .WithSummary("Create a new PayToFeature Basic Price")
        .WithDescription("Creates a new basic price configuration for PayToFeature with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }
}