using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
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
                var result = await service.GetPlansByVerticalAndCategoryAsync(verticalTypeId, categoryId, cancellationToken);
                if (result is null)
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = $"Plan with VerticalTypeId '{verticalTypeId}' and CategoryId '{categoryId}' not found.",
                        Status = StatusCodes.Status404NotFound
                    });

                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message, StatusCodes.Status500InternalServerError);
            }
           })
        .WithName("GetPayToPublish")
        .WithTags("PayToPublish")
        .WithSummary("Get a PayToPublish by Vertical and Category")
        .WithDescription("Get a new PayToPublish with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
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
                var result = await service.GetAllPlansAsync(cancellationToken);
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
        .WithDescription("Get All  new PayToPublish with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
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
        .WithSummary("Update a new PayToPublish")
        .WithDescription("Update a new PayToPublish with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
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
        .WithSummary("Delete  a PayToPublish ")
        .WithDescription("Deletes a new PayToPublish with the provided details.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }
   
}
