using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IAddonService;
using static QLN.Common.DTO_s.AddonDto;


namespace QLN.Common.Infrastructure.CustomEndpoints.AddonEndpoint
{
    public static class AddonEndpoints
    {
        public static RouteGroupBuilder MapQuantitiesEndpoints(this RouteGroupBuilder group)
        {
            // Get all quantities
            group.MapGet("/quantities", async Task<IResult> (
                [FromServices] IAddonService service,
                [FromServices] ILogger<IAddonService> logger,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    logger.LogInformation("Retrieving all quantities");
                    var quantities = await service.GetAllQuantitiesAsync();
                    return TypedResults.Ok(quantities);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving quantities");
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An error occurred while retrieving quantities.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetAllQuantities")
            .WithTags("Addons")
            .WithSummary("Get all quantities")
            .WithDescription("Retrieves all available quantities from the system.")
            .Produces<IEnumerable<AddonDto.Quantities>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Create quantity
            group.MapPost("/quantities", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                CreateQuantityRequest request,
                [FromServices] IAddonService service,
                [FromServices] ILogger<IAddonService> logger,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(request.QuantitiesName))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "UnitName is required and cannot be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    await service.CreateQuantityAsync(request);

                    return TypedResults.Ok("Quantity created successfully.");
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
            .WithName("CreateQuantity")
            .WithTags("Addons")
            .WithSummary("Create a new quantity")
            .WithDescription("Creates a new quantity unit in the system.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapCurrenciesEndpoints(this RouteGroupBuilder group)
        {
            // Create currency
            group.MapPost("/currencies", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                CreateCurrencyRequest request,
                [FromServices] IAddonService service,
                [FromServices] ILogger<IAddonService> logger,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(request.CurrencyName))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "CurrencyName is required and cannot be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    await service.CreateCurrencyAsync(request);

                    return TypedResults.Ok("Currency created successfully.");
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
            .WithName("CreateCurrency")
            .WithTags("Addons")
            .WithSummary("Create a new currency")
            .WithDescription("Creates a new currency in the system.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapUnitCurrenciesEndpoints(this RouteGroupBuilder group)
        {



            // Get unit currencies by unit ID
            group.MapGet("/quantites-currencies/by-quantites/{quantityId:Guid}", async Task<IResult> (
     Guid quantityId, // ✅ Fixed
     [FromServices] IAddonService service,
     [FromServices] ILogger<IAddonService> logger,
     CancellationToken cancellationToken = default) =>
            {
                try
                {
                    logger.LogInformation("Retrieving unit currencies for unit ID: {UnitId}", quantityId);
                    var unitCurrencies = await service.GetByquantityIdAsync(quantityId);

                    if (unitCurrencies == null || !unitCurrencies.Any())
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No unit currencies found for UnitId '{quantityId}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(unitCurrencies);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving unit currencies for unit ID: {UnitId}", quantityId);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An error occurred while retrieving unit currencies.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
 .WithName("GetquantitiesCurrenciesByUnitId")
 .WithTags("Addons")
 .WithSummary("Get quantities currencies by unit ID")
 .WithDescription("Retrieves all unit currency mappings for a specific unit ID.")
 .Produces<IEnumerable<AddonDto.UnitCurrency>>(StatusCodes.Status200OK)
 .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
 .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
 .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // Create unit currency
            group.MapPost("/quantities-currencies", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                CreateUnitCurrencyRequest request,
                [FromServices] IAddonService service,
                [FromServices] ILogger<IAddonService> logger,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                  

                    await service.CreatequantityCurrencyAsync(request);

                    return TypedResults.Ok("Unit currency created successfully.");
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
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Referenced Entity Not Found",
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
            .WithName("CreatequantitiesCurrency")
            .WithTags("Addons")
            .WithSummary("Create a new quantity currency mapping")
            .WithDescription("Creates a new mapping between a quantity unit and currency.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
