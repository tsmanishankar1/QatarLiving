using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Auditlog;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.Model;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json;

namespace QLN.Common.Infrastructure.CustomEndpoints.ServiceEndpoints
{
    public static class ServiceEndpoints
    {
        const string ModuleName = "Services";
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
            .AllowAnonymous()
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
                CategoryDto dto,
                IServices service,
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
                CategoryDto dto,
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
                string? vertical,
                string? subVertical,
                IServices service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetAllCategories(vertical, subVertical, cancellationToken);
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
            .Produces<List<CategoryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceCategoryGetByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/getbycategoryid/{id:long}", async Task<Results<
                Ok<CategoryDto>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                long id,
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
            .Produces<CategoryDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceAdEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
                ServiceDto dto,
                IServices service,
                AuditLogger auditLogger,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                string? uid = "unknown";
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
                    uid = userData.GetProperty("uid").GetString();
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

                    var result = await service.CreateServiceAd(uid, userName, dto, cancellationToken);
                    await auditLogger.LogAuditAsync(
                       module: ModuleName,
                       httpMethod: "POST",
                       apiEndpoint: "/api/service/create",
                       message: "Service ad created successfully",
                       createdBy: uid,
                       payload: dto,
                       cancellationToken: cancellationToken
                   );

                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/create", ex, uid, cancellationToken);
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Conflict",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/create", ex, uid, cancellationToken);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/create", ex, uid, cancellationToken);
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
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            group.MapPost("/createbyuserid", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
            ServiceRequest dto,
            [FromQuery] string uid,
            [FromQuery] string userName,
            IServices service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            {
                try
                {
                    dto.CreatedBy = uid;
                    dto.userName = userName;
                    var result = await service.CreateServiceAd(uid, userName, dto, cancellationToken);
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
                catch (ConflictException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Conflict",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
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
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceAdUpdateEndpoints(this RouteGroupBuilder group)
        {
            group.MapPut("/update", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
                Services dto,
                HttpContext httpContext,
                AuditLogger auditLogger,
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
                    await auditLogger.LogAuditAsync(
                        module: ModuleName,
                        httpMethod: "PUT",
                        apiEndpoint: "/api/service/update",
                        message: "Service ad updated successfully",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: cancellationToken
                    );
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/update", ex, dto.UpdatedBy, cancellationToken);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message
                    });
                }
                catch (ConflictException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/update", ex, dto.UpdatedBy, cancellationToken);
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Conflict",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/update", ex, dto.UpdatedBy, cancellationToken);
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
             .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            group.MapPut("/updatebyuserid", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
            Services dto,
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
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message
                    });
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Conflict",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
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
             .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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
                    var result = await service.GetAllAsync(ConstantValues.IndexNames.ServicesIndex, request);
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
            .Produces<List<Services>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServiceGetByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/getbyid/{id:long}", async Task<Results<Ok<Services>, NotFound<ProblemDetails>, ProblemHttpResult>> (
                long id,
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
            .Produces<Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceGetBySlugEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/getbyslug/{slug}", async Task<Results<Ok<Services>, NotFound<ProblemDetails>, ProblemHttpResult>> (
                string? slug,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetServiceAdBySlug(slug, cancellationToken);
                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Service Ad Not Found",
                            Detail = "No service ad found with Slug",
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
            .WithName("GetServiceAdBySlug")
            .WithTags("Service")
            .WithSummary("Get a service ad by Slug")
            .WithDescription("Retrieves a specific service ad by its unique identifier. If not found, returns 404.")
            .Produces<Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapDetailedGetByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/getbyserviceid/{slug}", async Task<Results<
                Ok<GetWithSimilarResponse<ServicesIndex>>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>> (

                string slug,
                [FromServices] ISearchService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetBySlugWithSimilarAsync<ServicesIndex>(
                        ConstantValues.IndexNames.ServicesIndex,
                        slug,
                        10
                    );

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Service Ad Not Found",
                            Detail = $"No service ad found with ID: {slug}",
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
            .Produces<GetWithSimilarResponse<Services>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceAdDeleteEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/delete", async Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>> (
                long id,
                HttpContext httpContext,
                AuditLogger auditLogger,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                string? uid = "unknown";
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
                    uid = userData.GetProperty("uid").GetString();
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
                    await auditLogger.LogAuditAsync(
                        module: ModuleName,
                        httpMethod: "POST",
                        apiEndpoint: "/api/service/delete",
                        message: "Service ad deleted successfully",
                        createdBy: uid,
                        payload: new { Id = id },
                        cancellationToken: cancellationToken
                    );
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/delete", ex, uid, cancellationToken);
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
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/delete", ex, uid, cancellationToken);
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
        public static RouteGroupBuilder MapGetAllWithPagination(this RouteGroupBuilder group)
        {
            group.MapPost("/getallwithpagination", async (
                [FromBody] BasePaginationQuery? dto,
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
                    var result = await service.GetAllServicesWithPagination(dto, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    return Results.Problem(
                        title: "Invalid request",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Failed to fetch services",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .AllowAnonymous()
            .WithName("GetAllServicesWithPagination")
            .WithTags("Service")
            .WithSummary("Get all services with pagination")
            .WithDescription("Returns paged service ads.")
            .Produces<PagedResponse<Services>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapPromoteEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/promote", async Task<IResult> (
                PromoteServiceRequest request,
                HttpContext httpContext,
                AuditLogger auditLogger,
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
                    request.UpdatedBy = uid;
                    if (uid == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    if (request == null || request.ServiceId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ServiceId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    //if (!userData.TryGetProperty("subscription", out var subscriptionElement) ||
                    //    !subscriptionElement.TryGetProperty("subscription_id", out var subscriptionIdElement))
                    //{
                    //    return TypedResults.Problem(new ProblemDetails
                    //    {
                    //        Title = "Unauthorized Access",
                    //        Detail = "Subscription ID is missing in token.",
                    //        Status = StatusCodes.Status403Forbidden
                    //    });
                    //}

                    //if (!Guid.TryParse(subscriptionIdElement.GetString(), out var subscriptionId))
                    //{
                    //    return TypedResults.Problem(new ProblemDetails
                    //    {
                    //        Title = "Invalid Subscription ID",
                    //        Detail = "Subscription ID in token is not a valid GUID.",
                    //        Status = StatusCodes.Status400BadRequest
                    //    });
                    //}
                    request.SubscriptionId = Guid.Parse("752ea67e-5fc3-4dae-ab96-4aa3822afc38");
                    var resultMessage = await service.PromoteService(request, uid, cancellationToken);

                    if (resultMessage == null)
                    {
                        return Results.Problem(
                            detail: "Service not found",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }
                    await auditLogger.LogAuditAsync(
                        module: ModuleName,
                        httpMethod: "POST",
                        apiEndpoint: "/api/service/promote",
                        message: "Service ad promoted successfully",
                        createdBy: uid,
                        payload: request,
                        cancellationToken: cancellationToken
                    );
                    return Results.Ok(resultMessage);
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/promote", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Invalid Request");
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/promote", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Request");
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/promote", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .WithName("PromoteService")
            .WithTags("Service")
            .WithSummary("Promote a service ad")
            .WithDescription("Promotes a service ad by paying a fee. Requires valid service ID.")
            .Produces<Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/promotebyuserid", async Task<IResult> (
                PromoteServiceRequest request,
                [FromQuery] string? uid,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (request == null || request.ServiceId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ServiceId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var resultMessage = await service.PromoteService(request, uid, cancellationToken);

                    if (resultMessage == null)
                    {
                        return Results.Problem(
                            detail: "Service not found",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }
                    return Results.Ok(resultMessage);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Invalid Request");
                }
                catch (InvalidDataException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Request");
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .ExcludeFromDescription()
            .WithName("PromoteServiceByUserId")
            .WithTags("Service")
            .WithSummary("Promote a service ad")
            .WithDescription("Promotes a service ad by paying a fee. Requires valid service ID.")
            .Produces<Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapFeatureEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/feature", async Task<IResult> (
                FeatureServiceRequest request,
                HttpContext httpContext,
                AuditLogger auditLogger,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                string uid = "unknown";
                try
                {
                    if (request == null || request.ServiceId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ServiceId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return Results.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();
                    var subscriptionId = new Guid("752ea67e-5fc3-4dae-ab96-4aa3822afc38");
                    //if (!userData.TryGetProperty("subscription", out var subscriptionElement) ||
                    //    !subscriptionElement.TryGetProperty("subscription_id", out var subscriptionIdElement) ||
                    //    !Guid.TryParse(subscriptionIdElement.GetString(), out var subscriptionId))
                    //{
                    //    return Results.Problem(new ProblemDetails
                    //    {
                    //        Title = "Invalid Subscription",
                    //        Detail = "Subscription ID is missing or invalid in the token.",
                    //        Status = StatusCodes.Status403Forbidden
                    //    });
                    //}

                    var result = await service.FeatureService(request, uid, subscriptionId, cancellationToken);

                    await auditLogger.LogAuditAsync(
                        module: "Service",
                        httpMethod: "POST",
                        apiEndpoint: "/api/service/feature",
                        message: $"Featured service with ID {request.ServiceId}",
                        createdBy: uid,
                        payload: request,
                        cancellationToken: cancellationToken
                    );

                    return Results.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync("Service", "/api/service/feature", ex, uid, cancellationToken);
                    return Results.Problem(ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Not Found");
                }
                catch (InvalidOperationException ex)
                {
                    await auditLogger.LogExceptionAsync("Service", "/api/service/feature", ex, uid, cancellationToken);
                    return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid Operation");
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Service", "/api/service/feature", ex, uid, cancellationToken);
                    return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Internal Server Error");
                }
            })
            .RequireAuthorization()
            .WithName("FeatureService")
            .WithTags("Service")
            .WithSummary("Feature a service ad")
            .WithDescription("Marks a service as featured and records subscription usage.")
            .Produces<Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/featurebyuserid", async Task<IResult> (
                FeatureServiceRequest request,
                [FromQuery] string? uid,
                Guid subscriptionId,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (request == null || request.ServiceId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ServiceId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.FeatureService(request, uid, subscriptionId, cancellationToken);

                    if (result == null)
                    {
                        return Results.Problem(
                            detail: "Service not found",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }

                    return Results.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.Problem(ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Not Found");
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid Operation");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Internal Server Error");
                }
            })
            .ExcludeFromDescription()
            .WithName("FeatureServiceByUserId")
            .WithTags("Service")
            .WithSummary("Feature a service ad (explicit user + subscription)")
            .WithDescription("Marks a service as featured when both user and subscription are explicitly provided.")
            .Produces<QLN.Common.Infrastructure.Model.Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapRefreshEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/refresh", async Task<IResult> (
                RefreshServiceRequest request,
                HttpContext httpContext,
                AuditLogger auditLogger,
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
                    request.UpdatedBy = uid;
                    if (uid == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    if (request == null || request.ServiceId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ServiceId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var resultMessage = await service.RefreshService(request, uid, cancellationToken);
                    if (resultMessage == null)
                    {
                        return Results.Problem(
                            detail: "Service not found",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }
                    await auditLogger.LogAuditAsync(
                        module: ModuleName,
                        httpMethod: "POST",
                        apiEndpoint: "/api/service/refresh",
                        message: "Service ad refreshed successfully",
                        createdBy: uid,
                        payload: request,
                        cancellationToken: cancellationToken
                    );
                    return Results.Ok(resultMessage);
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/refresh", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Invalid Request");
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/refresh", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Request");
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/refresh", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .WithName("RefreshService")
            .WithTags("Service")
            .WithSummary("Refresh a service ad")
            .WithDescription("Refreshes a service ad by paying a fee. Requires valid service ID.")
            .Produces<Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/refreshbyuserid", async Task<IResult> (
                RefreshServiceRequest request,
                [FromQuery] string? uid,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (request == null || request.ServiceId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ServiceId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var resultMessage = await service.RefreshService(request, uid, cancellationToken);
                    if (resultMessage == null)
                    {
                        return Results.Problem(
                            detail: "Service not found",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }
                    return Results.Ok(resultMessage);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Invalid Request");
                }
                catch (InvalidDataException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Request");
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .ExcludeFromDescription()
            .WithName("RefreshServiceByUserId")
            .WithTags("Service")
            .WithSummary("Refresh a service ad")
            .WithDescription("Refreshes a service ad by paying a fee. Requires valid service ID.")
            .Produces<Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapPublishEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/publish", async Task<IResult> (
                PublishServiceRequest request,
                HttpContext httpContext,
                AuditLogger auditLogger,
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
                    if (uid == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    request.UpdatedBy = uid;
                    var result = await service.PublishService(request, uid, cancellationToken);
                    if (result == null)
                    {
                        return Results.Problem(
                            detail: "Service not found.",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }
                    await auditLogger.LogAuditAsync(
                        module: ModuleName,
                        httpMethod: "POST",
                        apiEndpoint: "/api/service/publish",
                        message: "Service ad published successfully",
                        createdBy: uid,
                        payload: request,
                        cancellationToken: cancellationToken
                    );
                    return Results.Ok(result);
                }
                catch (ConflictException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/publish", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Conflict");
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/publish", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Invalid Request");
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/publish", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Request");
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/publish", ex, request.UpdatedBy, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .WithName("PublishService")
            .WithTags("Service")
            .WithSummary("Publish a service ad")
            .WithDescription("Publishes a service ad if it's not already published and follows category rules.")
            .Produces<Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/publishbyuserid", async Task<IResult> (
                PublishServiceRequest request,
                [FromQuery] string? uid,
                IServices service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.PublishService(request, uid, cancellationToken);
                    if (result == null)
                    {
                        return Results.Problem(
                            detail: "Service not found.",
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Service Not Found");
                    }

                    return Results.Ok(result);
                }
                catch (ConflictException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Conflict");
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Invalid Request");
                }
                catch (InvalidDataException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Request");
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .ExcludeFromDescription()
            .WithName("PublishServiceByUserId")
            .WithTags("Service")
            .WithSummary("Publish a service ad")
            .WithDescription("Publishes a service ad if it's not already published and follows category rules.")
            .Produces<Services>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapBulkActionsEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/moderatebulk", async Task<Results<
                    Ok<List<Services>>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult
                >> (
                    BulkModerationRequest req,
                    HttpContext httpContext,
                    AuditLogger auditLogger,
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
                    await auditLogger.LogAuditAsync(
                        module: ModuleName,
                        httpMethod: "POST",
                        apiEndpoint: "/api/service/moderatebulk",
                        message: "Bulk moderation action performed successfully",
                        createdBy: uid,
                        payload: req,
                        cancellationToken: ct
                    );
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/moderatebulk", ex, uid, ct);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ConflictException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/moderatebulk", ex, uid, ct);
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Conflict",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/service/moderatebulk", ex, uid, ct);
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
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/moderatebulkbyuserid", async Task<Results<
               Ok<List<Services>>,
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
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Conflict",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
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
           .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapServicesFeaturedItemEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/featured-services", async Task<IResult> (
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
                var searchReq = new CommonSearchRequest
                {
                    Filters = new Dictionary<string, object>
                    {
                        { "IsFeatured", true }
                    }
                };

                try
                {
                    CommonSearchResponse response = await searchSvc.SearchAsync(
                        ConstantValues.Verticals.Services,
                        searchReq
                    );

                    var list = response.ServicesItems ?? new List<ServicesIndex>();

                    return TypedResults.Ok(list);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
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
            .AllowAnonymous()
            .WithName($"GetFeatured_{ConstantValues.Verticals.Services}_Items")
            .WithTags("Service")
            .WithSummary("Get all featured service items")
            .WithDescription("Fetches every ServicesIndex document where IsFeatured = true.")
            .Produces<IEnumerable<ServicesIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapServiceCountEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getcounts", async Task<Results<Ok<SubscriptionBudgetDto>, ProblemHttpResult>> (
                HttpContext httpContext,
                [FromServices] IServices service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    // Get "subscriptions" claim directly from token
                    var subscriptionsClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "subscriptions")?.Value;
                    if (string.IsNullOrEmpty(subscriptionsClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "Subscriptions information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }

                    // Deserialize array of subscriptions
                    var subscriptions = JsonSerializer.Deserialize<JsonElement>(subscriptionsClaim);
                    if (subscriptions.ValueKind != JsonValueKind.Array || subscriptions.GetArrayLength() == 0)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Invalid Token",
                            Detail = "No valid subscription found in token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }

                    // Take first subscription (or select specific one as needed)
                    var subscription = subscriptions[0];

                    if (!subscription.TryGetProperty("Id", out var subscriptionIdElement))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Invalid Subscription",
                            Detail = "Subscription Id is missing in token.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var subscriptionIdString = subscriptionIdElement.GetString();
                    if (!Guid.TryParse(subscriptionIdString, out var subscriptionId))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Invalid Subscription ID",
                            Detail = "Subscription ID in token is not a valid GUID.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    // Call your service using subscriptionId
                    var counts = await service.GetSubscriptionBudgetsAsync(subscriptionId, cancellationToken);
                    return TypedResults.Ok(counts);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetServiceStatusCountsbysubscription")
            .WithTags("Service")
            .WithSummary("Get subscription budget details")
            .WithDescription("Returns budget details for the logged-in subscription ID.")
            .Produces<SubscriptionBudgetDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/getbudgets", async Task<Results<Ok<SubscriptionBudgetDto>, ProblemHttpResult>> (
                 [FromBody] SubscriptionIdRequest dto,
                 [FromServices] IServices service,
                 CancellationToken cancellationToken
             ) =>
            {
                try
                {
                    var counts = await service.GetSubscriptionBudgetsAsync(dto.SubscriptionId, cancellationToken);
                    return TypedResults.Ok(counts);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("GetServiceStatusCounts")
            .WithTags("Service");

            return group;
        }


        //public static RouteGroupBuilder MapServiceCountEndpoints(this RouteGroupBuilder group)
        //{
        //    group.MapGet("/getcounts", async Task<Results<Ok<SubscriptionBudgetDto>, ProblemHttpResult>> (
        //        [FromServices] IServices service,
        //        CancellationToken cancellationToken
        //    ) =>
        //    {
        //        try
        //        {
        //            // Hardcoded subscriptionId for testing
        //            var subscriptionId = new Guid("48887e22-782a-4825-a0b6-bd27259ef554");

        //            var counts = await service.GetSubscriptionBudgetsAsync(subscriptionId, cancellationToken);
        //            return TypedResults.Ok(counts);
        //        }
        //        catch (Exception ex)
        //        {
        //            return TypedResults.Problem("Internal Server Error", ex.Message);
        //        }
        //    })
        //    .WithName("GetServiceStatusCountsbysubscription")
        //    .WithTags("Service")
        //    .WithSummary("Get subscription budget details")
        //    .WithDescription("Returns budget details for the hardcoded subscription ID (testing).")
        //    .Produces<SubscriptionBudgetDto>(StatusCodes.Status200OK)
        //    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        //    group.MapPost("/getbudgets", async Task<Results<Ok<SubscriptionBudgetDto>, ProblemHttpResult>> (
        //        [FromBody] SubscriptionIdRequest dto,
        //        [FromServices] IServices service,
        //        CancellationToken cancellationToken
        //    ) =>
        //    {
        //        try
        //        {
        //            var counts = await service.GetSubscriptionBudgetsAsync(dto.SubscriptionId, cancellationToken);
        //            return TypedResults.Ok(counts);
        //        }
        //        catch (Exception ex)
        //        {
        //            return TypedResults.Problem("Internal Server Error", ex.Message);
        //        }
        //    })
        //    .ExcludeFromDescription()
        //    .WithName("GetServiceStatusCounts")
        //    .WithTags("Service");

        //    return group;
        //}

        public static RouteGroupBuilder MapServiceCountbySubverticalEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getcountsbyvertical", async Task<Results<Ok<SubscriptionBudgetDto>, ProblemHttpResult>> (
                HttpContext httpContext,
                [FromServices] IServices service,
                [FromQuery] int verticalId,
                [FromQuery] int? subverticalId,    // <-- now nullable
                CancellationToken cancellationToken)
            =>
            {
                try
                {
                    // Get "subscriptions" claim directly from token
                    var subscriptionsClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "subscriptions")?.Value;
                    if (string.IsNullOrEmpty(subscriptionsClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "Subscriptions information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }

                    // Deserialize subscriptions (can be object or array)
                    var subscriptionElement = JsonSerializer.Deserialize<JsonElement>(subscriptionsClaim);
                    JsonElement subscription;

                    if (subscriptionElement.ValueKind == JsonValueKind.Array)
                    {
                        if (subscriptionElement.GetArrayLength() == 0)
                        {
                            return TypedResults.Problem(new ProblemDetails
                            {
                                Title = "Invalid Token",
                                Detail = "No valid subscription found in token.",
                                Status = StatusCodes.Status403Forbidden
                            });
                        }
                        subscription = subscriptionElement[0];
                    }
                    else if (subscriptionElement.ValueKind == JsonValueKind.Object)
                    {
                        subscription = subscriptionElement;
                    }
                    else
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Invalid Token",
                            Detail = "Invalid subscriptions format in token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }

                    // Extract Id property
                    if (!subscription.TryGetProperty("Id", out var subscriptionIdElement))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "Subscription Id is missing in token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }

                    if (!Guid.TryParse(subscriptionIdElement.GetString(), out var subscriptionId))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Invalid Subscription ID",
                            Detail = "Subscription ID in token is not a valid GUID.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    // Pass subscriptionId, verticalId, and subverticalId to your service
                    var counts = await service.GetSubscriptionBudgetsAsyncBySubVertical(subscriptionId, verticalId, subverticalId, cancellationToken);
                    return TypedResults.Ok(counts);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetServiceStatusCountsbySubvertical")
            .WithTags("Service")
            .WithSummary("Get subscription budget details with vertical/subvertical filter")
            .WithDescription("Returns budget details for the logged-in subscription ID. If subverticalId is provided, filters by both verticalId and subverticalId; otherwise, filters by verticalId only.")
            .Produces<SubscriptionBudgetDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Post endpoint for internal service calls
            group.MapPost("/getbudgetsbysubvertical", async Task<Results<Ok<SubscriptionBudgetDto>, ProblemHttpResult>> (
                [FromBody] SubscriptionRequest dto,
                [FromServices] IServices service,
                CancellationToken cancellationToken)
            =>
            {
                try
                {
                    var counts = await service.GetSubscriptionBudgetsAsyncBySubVertical(dto.SubscriptionId, dto.VerticalId, dto.SubVerticalId, cancellationToken);
                    return TypedResults.Ok(counts);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("GetServiceStatusCountsSubvertical")
            .WithTags("Service");

            return group;
        }







    }
}
