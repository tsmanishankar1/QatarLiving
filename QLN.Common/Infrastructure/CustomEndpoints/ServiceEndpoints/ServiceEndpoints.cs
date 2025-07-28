using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using Microsoft.AspNetCore.Builder;
using QLN.Common.Infrastructure.IService.IService;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.Infrastructure.CustomEndpoints.ServiceEndpoints
{
    public static class ServiceEndpoints
    {
        public static RouteGroupBuilder MapServiceSearch(this RouteGroupBuilder group)
        {
            group.MapPost("/search", async (
            [FromBody] CommonSearchRequest req,
            [FromServices] ISearchService svc,
            [FromServices] ILoggerFactory logFac
            ) =>
            {
                var logger = logFac.CreateLogger("ServicesEndpoints");

                var validationContext = new ValidationContext(req);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(req, validationContext, validationResults, validateAllProperties: true))
                {
                    var errorMessages = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
                    logger.LogWarning("Validation failed: {Errors}", errorMessages);

                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Failed",
                        Detail = errorMessages,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/services/search"
                    });
                }

                try
                {
                    var results = await svc.SearchAsync(ConstantValues.IndexNames.ServicesIndex, req);
                    return Results.Ok(results);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid search request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/services/search"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception during search");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/services/search"
                    );
                }
            })
            .WithName("SearchServicesItems")
            .WithTags("Service")
            .WithSummary("Search Services Items")
            .Produces<IEnumerable<ServicesIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }
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
                try
                {
                    var result = await service.GetAllCategories(cancellationToken);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Internal Server Error", ex.Message);
                }
            })
            .AllowAnonymous()
            .WithTags("Service")
            .WithName("GetAllServiceCategories")
            .WithDescription("Retrieves all service categories from the system. " +
                             "This endpoint returns a list of all available service categories, including their subcategories.")
            .WithSummary("Get all service categories")
            .Produces<List<ServicesCategory>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceCategoryGetByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/getbycategoryid/{id:guid}", async Task<Results<
                Ok<ServicesCategory>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                Guid id,
                IServices service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetCategoryById(id, cancellationToken);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Category Id Not Found",
                            Detail = $"No Category found with ID: {id}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    var details = new ProblemDetails
                    {
                        Title = "Category id Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    };
                    return TypedResults.NotFound(details);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .AllowAnonymous()
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
            group.MapPost("/create", async Task<Results<Ok<ServicesDto>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
                ServicesDto dto,
                IServices service,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    var userName = userData.GetProperty("name").GetString();
                    if (uid == null && userName == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID or username could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    var id = Guid.NewGuid();
                    dto.Id = id;
                    dto.CreatedBy = uid;
                    dto.CreatedAt = DateTime.UtcNow;
                    dto.UpdatedBy = null;
                    dto.UpdatedAt = null;
                    dto.RefreshExpiryDate = null;
                    dto.PromotedExpiryDate = null;
                    dto.FeaturedExpiryDate = null;
                    dto.PublishedDate = null;
                    dto.ExpiryDate = null;
                    dto.IsFeatured = false;
                    dto.IsPromoted = false;
                    dto.IsRefreshed = false;
                    dto.UserName = userName;
                    var result = await service.CreateServiceAd(uid, dto, cancellationToken);
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
            .Produces<ServicesDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            group.MapPost("/createbyuserid", async Task<Results<Ok<ServicesDto>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
            ServicesDto dto,
            IServices service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            {
                try
                {
                    if (dto.CreatedBy == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "CreatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.CreateServiceAd(dto.CreatedBy, dto, cancellationToken);
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
            .ExcludeFromDescription()
            .WithName("CreateExcludeServiceAd")
            .WithTags("Service")
            .WithSummary("Create a new service ad")
            .WithDescription("Creates a new service ad with the provided details. " +
                                 "The ad must include a valid category and description.")
            .Produces<ServicesDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceAdUpdateEndpoints(this RouteGroupBuilder group)
        {
            group.MapPut("/update", async Task<Results<Ok<string>, NotFound, ProblemHttpResult>> (
                ServicesDto dto,
                HttpContext httpContext,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    var userName = userData.GetProperty("name").GetString();
                    if (uid == null && userName == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID or username could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    dto.UpdatedBy = uid;
                    dto.UpdatedAt = DateTime.UtcNow;
                    dto.UserName = userName;
                    var result = await service.UpdateServiceAd(uid, dto, cancellationToken);
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
            group.MapPut("/updatebyuserid", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
            ServicesDto dto,
            HttpContext httpContext,
            IServices service,
            CancellationToken cancellationToken) =>
            {
                try
                {
                    if (dto.UpdatedBy == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "CreatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.UpdateServiceAd(dto.UpdatedBy, dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
             .ExcludeFromDescription()
             .WithTags("Service")
             .WithName("UpdateExcludeServiceAd")
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
            group.MapPost("/getall", static async Task<Results<Ok<AllServices>, ProblemHttpResult>>
            (
                [FromServices] ISearchService service,
                [FromBody] CommonSearchRequest request,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetAllAsync(ConstantValues.IndexNames.ServicesIndex,request);
                    var allservice = new AllServices
                    {
                        TotalCount = result.TotalCount,
                        ServicesItems = result.ServicesItems
                    };
                    return TypedResults.Ok(allservice);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllServiceAds")
            .WithTags("Service")
            .WithSummary("Get all service ads")
            .WithDescription("Retrieves all service ads from the system. " +
                             "This endpoint returns a list of all available service ads, including their details.")
            .Produces<List<ServicesDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceGetByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/getbyid/{id:guid}", async Task<Results<Ok<ServicesDto>, NotFound<ProblemDetails>, ProblemHttpResult>> (
                Guid id,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetServiceAdById(id, cancellationToken);
                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Service Ad Not Found",
                            Detail = $"No service ad found with ID: {id}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    var details = new ProblemDetails
                    {
                        Title = "Service Ad Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    };
                    return TypedResults.NotFound(details);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .AllowAnonymous()
            .WithName("GetServiceAdById")
            .WithTags("Service")
            .WithSummary("Get a service ad by ID")
            .WithDescription("Retrieves a specific service ad by its unique identifier. If not found, returns 404.")
            .Produces<ServicesDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapDetailedGetByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/getbyserviceid/{id:guid}", async Task<Results<
                Ok<GetWithSimilarResponse<ServicesIndex>>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>> (

                Guid id,
                [FromServices] ISearchService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetByIdWithSimilarAsync<ServicesIndex>(
                        ConstantValues.IndexNames.ServicesIndex,
                        id.ToString(),
                        10
                    );

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Service Ad Not Found",
                            Detail = $"No service ad found with ID: {id}",
                            Status = 404
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = 404
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = ex.Message,
                        Status = 500
                    });
                }
            })
            .AllowAnonymous()
            .WithName("GetDetailedServiceAdById")
            .WithTags("Service")
            .WithSummary("Get a service ad by ID")
            .WithDescription("Retrieves a specific service ad by its unique identifier. If not found, returns 404.")
            .Produces<GetWithSimilarResponse<ServicesDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceAdDeleteEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/delete", async Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>> (
                Guid id,
                HttpContext httpContext,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();

                    if (string.IsNullOrEmpty(uid))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    var result = await service.DeleteServiceAdById(uid, id, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    var details = new ProblemDetails
                    {
                        Title = "Service Ad Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    };
                    return TypedResults.NotFound(details);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("DeleteServiceAd")
            .WithTags("Service")
            .WithSummary("Soft delete service ad")
            .WithDescription("Soft deletes a service ad.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/deletebyuserid", async Task<Results<Ok<string>, ProblemHttpResult>> (
                [FromBody] DeleteServiceRequest dto,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.DeleteServiceAdById(dto.UpdatedBy, dto.Id, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("DeleteServiceAdByUserId")
            .WithTags("Service")
            .WithSummary("Soft delete service ad by user id")
            .WithDescription("Soft deletes a service ad directly by providing Id and UpdatedBy.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetServicesByStatusEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/getbystatus", async (
                [FromBody] ServiceStatusQuery dto,
                IServices service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    dto.PageNumber ??= 1;
                    dto.PerPage ??= 12;
                    if (dto.PageNumber <= 0 || dto.PerPage <= 0)
                    {
                        return Results.Problem(
                            title: "Invalid pagination parameters",
                            detail: "PageNumber and PerPage must be greater than zero.",
                            statusCode: StatusCodes.Status400BadRequest
                        );
                    }
                    if (dto.Status == null)
                    {
                        return Results.Problem(
                            title: "Invalid request",
                            detail: "Status is required.",
                            statusCode: StatusCodes.Status400BadRequest
                        );
                    }
                    var result = await service.GetServicesByStatusWithPagination(dto, cancellationToken);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Failed to fetch services by status",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .AllowAnonymous()
            .WithName("GetServicesByStatus")
            .WithTags("Service")
            .WithSummary("Get services by status (with pagination)")
            .WithDescription("Returns paged service ads matching the given status (Published, Unpublished, etc).")
            .Produces<PagedResponse<ServicesDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapPromoteEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/promote", async Task<IResult> (
                PromoteServiceRequest request,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (request == null || request.ServiceId == Guid.Empty)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ServiceId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var resultMessage = await service.PromoteService(request, cancellationToken);

                    if (resultMessage == null)
                    {
                        return Results.Problem(
                            detail: "Service not found",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }
                    return Results.Ok(resultMessage);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .AllowAnonymous()
            .WithName("PromoteService")
            .WithTags("Service")
            .WithSummary("Promote a service ad")
            .WithDescription("Promotes a service ad by paying a fee. Requires valid service ID.")
            .Produces<ServicesDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapFeatureEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/feature", async Task<IResult> (
                FeatureServiceRequest request,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (request == null || request.ServiceId == Guid.Empty)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ServiceId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var resultMessage = await service.FeatureService(request, cancellationToken);

                    if (resultMessage == null)
                    {
                        return Results.Problem(
                            detail: "Service not found",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }

                    return Results.Ok(resultMessage);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .AllowAnonymous()
            .WithName("FeatureService")
            .WithTags("Service")
            .WithSummary("Feature a service ad")
            .WithDescription("Features a service ad by paying a fee. Requires valid service ID.")
            .Produces<ServicesDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapRefreshEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/refresh", async Task<IResult> (
                RefreshServiceRequest request,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (request == null || request.ServiceId == Guid.Empty)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ServiceId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var resultMessage = await service.RefreshService(request, cancellationToken);
                    if (resultMessage == null)
                    {
                        return Results.Problem(
                            detail: "Service not found",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }
                    return Results.Ok(resultMessage);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .AllowAnonymous()
            .WithName("RefreshService")
            .WithTags("Service")
            .WithSummary("Refresh a service ad")
            .WithDescription("Refreshes a service ad by paying a fee. Requires valid service ID.")
            .Produces<ServicesDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapPublishEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/publish", async Task<IResult> (
                [FromQuery] Guid id, 
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.PublishService(id, cancellationToken);
                    if (result == null)
                    {
                        return Results.Problem(
                            detail: "Service not found.",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .AllowAnonymous()
            .WithName("PublishService")
            .WithTags("Service")
            .WithSummary("Publish a service ad")
            .WithDescription("Publishes a service ad if it's not already published and follows category rules.")
            .Produces<ServicesDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapBulkActionsEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/moderatebulk", async Task<Results<
                    Ok<List<ServicesDto>>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult
                >> (
                    BulkModerationRequest req,
                    HttpContext httpContext,
                    IServices service,
                    CancellationToken ct
                ) =>
            {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    var userName = userData.GetProperty("name").GetString();
                    if (uid == null && userName == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID or username could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    if (!req.AdIds.Any())
                        return TypedResults.BadRequest(new ProblemDetails { Title = "No ads selected." });

                    if (req.Action == BulkModerationAction.Remove && string.IsNullOrWhiteSpace(req.Reason))
                        return TypedResults.BadRequest(new ProblemDetails { Title = "Reason required for removal." });
                req.UpdatedBy = uid;
                    try
                    {
                        var result = await service.ModerateBulkService(req, ct);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(ex.Message);
                    }
                })
                .WithName("BulkModerateServices")
                .WithTags("Service")
                .WithSummary("Bulk moderate service ads")
                .WithDescription("Performs bulk moderation actions (approve, publish, unpublish, remove) on selected service ads. " +
                                 "Requires a list of ad IDs and the action to perform. " +
                                 "If removing, a reason must be provided.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            group.MapPost("/moderatebulkbyuserid", async Task<Results<
               Ok<List<ServicesDto>>,
               BadRequest<ProblemDetails>,
               ProblemHttpResult
           >> (
               BulkModerationRequest req,
               HttpContext httpContext,
               IServices service,
               CancellationToken ct
           ) =>
            {
                try
                {
                    if (req.UpdatedBy == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "UpdatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.ModerateBulkService(req, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })
           .ExcludeFromDescription()
           .WithName("BulkModerateServicesbyuserid")
           .WithTags("Service")
           .WithSummary("Bulk moderate service ads")
           .WithDescription("Performs bulk moderation actions (approve, publish, unpublish, remove) on selected service ads. " +
                            "Requires a list of ad IDs and the action to perform. " +
                            "If removing, a reason must be provided.")
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
    }
}
