using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using Microsoft.AspNetCore.Builder;
using QLN.Common.Infrastructure.IService.IService;

namespace QLN.Common.Infrastructure.CustomEndpoints.ServiceEndpoints
{
    public static class ServiceEndpoints
    {
        public static RouteGroupBuilder MapServiceCategoryEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/createcategory", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                ServicesCategory dto,
                IServices service,
                HttpContext httpContext,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.CreateCategory(dto, cancellationToken);
                    return TypedResults.Ok(result);
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
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("CreateServiceCategory")
            .WithTags("Service")
            .WithSummary("Create a new service category")
            .WithDescription("Creates a new service category with the provided details. " +
                             "The category must include at least one L1 category and each L1 category must have at least one L2 category.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceCategoryUpdateEndpoints(this RouteGroupBuilder group)
        {
            group.MapPut("/updatecategory", async Task<Results<
                Ok<string>,
                NotFound,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                ServicesCategory dto,
                IServices service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.UpdateCategory(dto, cancellationToken);
                    return TypedResults.Ok(result);
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
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("UpdateServiceCategory")
            .WithTags("Service")
            .WithSummary("Update an existing service category")
            .WithDescription("Updates an existing service category with the provided details. " +
                             "The category must already exist in the system.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceCategoryGetAllEndpoints(this RouteGroupBuilder group)
        { 
            group.MapGet("/getallcategories", async (
                IServices service,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await service.GetAllCategories(cancellationToken);
                return Results.Ok(result);
            })
            .WithTags("Service")
            .WithName("GetAllServiceCategories")
            .WithDescription("Retrieves all service categories from the system. " +
                             "This endpoint returns a list of all available service categories, including their subcategories.")
            .WithSummary("Get all service categories")
            .Produces<List<ServicesCategory>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceCategoryGetByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/getbycategoryid/{id:guid}", async Task<Results<
                Ok<ServicesCategory>,
                NotFound,
                ProblemHttpResult>>
            (
                Guid id,
                IServices service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var category = await service.GetCategoryById(id, cancellationToken);
                    return category is not null
                        ? TypedResults.Ok(category)
                        : TypedResults.NotFound();
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetServiceCategoryById")
            .WithTags("Service")
            .WithSummary("Get a service category by ID")
            .WithDescription("Retrieves a specific service category by its unique identifier. " +
                             "If the category does not exist, a 404 Not Found response is returned.")
            .Produces<ServicesCategory>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceAdEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
                ServicesDto dto,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.CreateServiceAd(dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("CreateServiceAd")
            .WithTags("Service")
            .WithSummary("Create a new service ad")
            .WithDescription("Creates a new service ad with the provided details. " +
                                 "The ad must include a valid category and description.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceAdUpdateEndpoints(this RouteGroupBuilder group)
        { 
            group.MapPut("/update", async Task<Results<Ok<string>, NotFound, ProblemHttpResult>> (
                ServicesDto dto,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.UpdateServiceAd(dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.NotFound();
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
             .WithTags("Service")
             .WithName("UpdateServiceAd")
             .WithSummary("Update an existing service ad")
             .WithDescription("Updates an existing service ad with the provided details. " +
                                 "The ad must already exist in the system.")
             .Produces<string>(StatusCodes.Status200OK)
             .Produces(StatusCodes.Status404NotFound)
             .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
             .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceGetAllEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/all", async (IServices service, CancellationToken cancellationToken) =>
            {
                var result = await service.GetAllServiceAds(cancellationToken);
                return TypedResults.Ok(result);
            })
            .WithName("GetAllServiceAds")
            .WithTags("Service")
            .WithSummary("Get all service ads")
            .WithDescription("Retrieves all service ads from the system. " +
                             "This endpoint returns a list of all available service ads, including their details.")
            .Produces<List<ServicesDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceGetByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/getbyid/{id:guid}", async Task<Results<Ok<ServicesDto>, NotFound>> (
                Guid id,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetServiceAdById(id, cancellationToken);
                return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
            })
            .WithName("GetServiceAdById")
            .WithTags("Service")
            .WithSummary("Get a service ad by ID")
            .WithDescription("Retrieves a specific service ad by its unique identifier. " +
                             "If the ad does not exist, a 404 Not Found response is returned.")
            .Produces<ServicesDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceAdDeleteEndpoint(this RouteGroupBuilder group)
        { 
            group.MapDelete("/delete/{id:guid}", async Task<Results<Ok<string>, NotFound, ProblemHttpResult>> (
                Guid id,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.DeleteServiceAdById(id, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException)
                {
                    return TypedResults.NotFound();
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("DeleteServiceAd")
            .WithTags("Service")
            .WithSummary("Delete a service ad by ID")
            .WithDescription("Deletes a service ad by its unique identifier. " +
                             "If the ad does not exist, a 404 Not Found response is returned.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
    }
}
