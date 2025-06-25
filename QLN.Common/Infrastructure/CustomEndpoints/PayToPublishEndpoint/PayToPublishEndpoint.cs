using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToPublishService;
using System.Security.Claims;

namespace QLN.Common.Infrastructure.CustomEndpoints.PayToPublishEndpoint;

public static class PayToPublishEndpoints
{
    public static RouteGroupBuilder MapCreatePayToPublishEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/add", async Task<Results<
            Ok<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            PayToPublishRequestDto request,
            IPayToPublishService service,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                await service.CreatePlanAsync(request, cancellationToken);
                return TypedResults.Ok("PayToPublish plan created successfully.");
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
        .WithName("CreatePayToPublish")
        .WithTags("PayToPublish")
        .WithSummary("Create a new PayToPublish")
        .WithDescription("Creates a new PayToPublish with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapGetPayToPublishEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/getpaytopublish", async Task<IResult> (
        [FromQuery] int verticalTypeId,
        [FromQuery] int categoryId,
        [FromServices] IPayToPublishService service,
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
        .WithName("GetPayToPublish")
        .WithTags("PayToPublish")
        .WithSummary("Get PayToPublish plans by Vertical and Category")
        .WithDescription("Get PayToPublish plans filtered by the provided vertical type and category.")
        .Produces<PayToPublishListResponseDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        return group;
    }

    public static RouteGroupBuilder MapGetAllPayToPublishEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/getAll", async Task<IResult> (
            [FromServices] IPayToPublishService service,
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
        .WithName("GetAllPayToPublishPlans")
        .WithTags("PayToPublish")
        .WithSummary("Get All PayToPublish Details")
        .WithDescription("Get all PayToPublish plans.")
        .Produces<List<PayToPublishResponseDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapUpatePayToPublishEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/update", async Task<Results<
            Ok<string>,
            NotFound<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            [FromHeader(Name = "PayToPublish-Id")] Guid planId,
            [FromBody] PayToPublishRequestDto request,
            [FromServices] IPayToPublishService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var updated = await service.UpdatePlanAsync(planId, request, cancellationToken);
                if (!updated)
                    return TypedResults.NotFound($"Plan with ID {planId} not found.");

                return TypedResults.Ok("PayToPublish plan updated successfully.");
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
        .WithName("UpdatePayToPublishPlans")
        .WithTags("PayToPublish")
        .WithSummary("Update a PayToPublish")
        .WithDescription("Update a PayToPublish with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapDeletePayToPublishEndpoints(this RouteGroupBuilder group)
    {
        group.MapDelete("/delete", async Task<IResult> (
            [FromHeader(Name = "PayToPublish-Id")] Guid planId,
            [FromServices] IPayToPublishService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var deleted = await service.DeletePlanAsync(planId, cancellationToken);
                if (!deleted)
                    return Results.NotFound("PayToPublish plan not found.");

                return Results.Ok("PayToPublish plan deleted successfully.");
            }
            catch (Exception ex)
            {
                return Results.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("DeletePayToPublishPlan")
        .WithTags("PayToPublish")
        .WithSummary("Delete a PayToPublish")
        .WithDescription("Deletes a PayToPublish with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapGetPayToPublishPaymentsByUserEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/paytopublish/user", async (
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
        .WithName("GetPayToPublishPaymentsByUser")
        .WithTags("PayToPublish")
        .WithSummary("Get PayToPublish payments by user")
        .Produces<List<PaymentDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapGetPayToPublishPaymentsByUserIdEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/paytopublish/user/{userId:guid}", async (
            Guid userId,
            [FromServices] IPayToPublishService service,
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
        .WithName("GetPayToPublishPaymentsByUserId")
        .WithTags("PayToPublish")
        .WithSummary("Get PayToPublish payments by userId")
        .Produces<List<PaymentDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }

    public static RouteGroupBuilder MapCreateBasicPriceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/basicprice/add", async Task<Results<
            Ok<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            BasicPriceRequestDto request,
            IPayToPublishService service,
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
        .WithName("CreateBasicPrice")
        .WithTags("PayToPublish")
        .WithSummary("Create a new Basic Price")
        .WithDescription("Creates a new basic price configuration with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        //group.MapGet("/basicprice/getbasicprice", async Task<IResult> (
        //[FromQuery] int verticalTypeId,
        //[FromQuery] int categoryId,
        //[FromServices] IPayToPublishService service,
        //CancellationToken cancellationToken) =>
        //{
        //    try
        //    {
        //        var result = await service.GetBasicPricesByVerticalAndCategoryAsync(verticalTypeId, categoryId, cancellationToken);
        //        if (result == null || !result.Any())
        //            return TypedResults.NotFound(new ProblemDetails
        //            {
        //                Title = "Not Found",
        //                Detail = $"Basic price with VerticalTypeId '{verticalTypeId}' and CategoryId '{categoryId}' not found.",
        //                Status = StatusCodes.Status404NotFound
        //            });

        //        return TypedResults.Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
        //    }
        //})
        //.WithName("GetBasicPriceByVerticalAndCategory")
        //.WithTags("PayToPublish")
        //.WithSummary("Get Basic Prices by Vertical and Category")
        //.WithDescription("Retrieves basic prices filtered by vertical type and category.")
        //.Produces<List<BasicPriceResponseDto>>(StatusCodes.Status200OK)
        //.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        //.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }
}