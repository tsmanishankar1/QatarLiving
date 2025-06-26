using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using System.Security.Claims;
using QLN.Common.Infrastructure.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Utilities;
using QLN.Common.Infrastructure.CustomException;
using System.ComponentModel.DataAnnotations;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Azure;
using static QLN.Common.DTO_s.ClassifiedsIndex;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class ClassifiedEndpoints
    {
        public static RouteGroupBuilder MapClassifiedEndpoints(this RouteGroupBuilder group)
        {
            // SEARCH
            group.MapPost("/search", async (
                    [FromBody] CommonSearchRequest req,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");

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
                        Instance = $"/api/classified/search"
                    });
                }

                try
                {
                    var results = await svc.SearchAsync(ConstantValues.ClassifiedsVertical, req);
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
                        Instance = $"/api/classified/search"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception during search");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classified/search"
                    );
                }
            })
            .WithName("SearchClassified")
            .WithTags("Classified")
            .WithSummary("Search classifieds")
            .Produces<IEnumerable<ClassifiedsIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
            
            
            // GET BY ID
            group.MapGet("/{id}", async (
                    [FromRoute] string id,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogWarning("GetById called with empty id");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/classified/{id}"
                    });
                }

                try
                {
                    var ad = await svc.GetByIdAsync<ClassifiedsIndex>(ConstantValues.Verticals.Classifieds, id);
                    if (ad is null)
                        return Results.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No document '{id}' in 'classifieds'.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = $"/api/classified/{id}"
                        });

                    return Results.Ok(ad);
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = $"No document '{id}' in '{ConstantValues.Verticals.Classifieds}'.",
                        Status = StatusCodes.Status404NotFound,
                        Instance = $"/api/classified/{id}"
                    });
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid GetById request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/classified/{id}"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GetById error");
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classified/{id}"
                    );
                }
            })
            .WithName("GetClassifiedById")
            .WithTags("Classified")
            .WithSummary("Get a classified by its ID")
            .Produces<ClassifiedsIndex>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapGet("/details/{id}", async (
                    [FromRoute] string id,
                    [FromQuery] int similarPageSize,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");

                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogWarning("GetDetails called with empty id");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{ConstantValues.Verticals.Classifieds}/details/{id}"
                    });
                }

                try
                {
                    var result = await svc.GetByIdWithSimilarAsync<ClassifiedsIndex>(
                        ConstantValues.Verticals.Classifieds,
                        id,
                        similarPageSize
                    );
                    return Results.Ok(result);
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = $"No document '{id}' in '{ConstantValues.Verticals.Classifieds}'.",
                        Status = StatusCodes.Status404NotFound,
                        Instance = $"/api/{ConstantValues.Verticals.Classifieds}/details/{id}"
                    });
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid arguments for details");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{ConstantValues.Verticals.Classifieds}/details/{id}"
                    });
                }
                catch (RequestFailedException ex)
                {
                    logger.LogError(ex, "Azure Search error on details");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway,
                        instance: $"/api/{ConstantValues.Verticals.Classifieds}/details/{id}"
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error on details");
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{ConstantValues.Verticals.Classifieds}/details/{id}"
                    );
                }
            })
            .WithName("GetClassifiedDetailsWithSimilar")
            .WithTags("Classified")
            .WithSummary("Get a classified plus similar items")
            .WithDescription("Returns the requested ClassifiedsIndex along with up to `similarPageSize` others sharing its L2/L1 category.");

            // UPLOAD
            group.MapPost("/upload", async (
                    [FromBody] ClassifiedsIndex doc,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                if (doc is null)
                {
                    logger.LogWarning("Upload called with null document");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document payload is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/classified/upload"
                    });
                }
                var indexDocument = new CommonIndexRequest
                {
                    VerticalName = ConstantValues.Verticals.Classifieds,
                    ClassifiedsItem = doc
                };
                try
                {
                    var msg = await svc.UploadAsync(indexDocument);
                    return Results.Ok(msg);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid upload request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/classified/upload"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Upload error");
                    return Results.Problem(
                        title: "Upload Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classified/upload"
                    );
                }
            })
            .WithName("UploadClassified")
            .WithTags("Classified")
            .WithSummary("Upload or create a classified item")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
            // added save search
            group.MapPost("/search/saveSearch", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                SaveSearchRequestDto dto,
                IClassifiedService service,
                HttpContext context
            ) =>
            {
                Guid userId = context.User.GetId();
                if (userId == null || userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search name is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (dto.SearchQuery == null || string.IsNullOrWhiteSpace(dto.SearchQuery.Text))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search query text is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var success = await service.SaveSearch(dto, userId);
                    if (success)
                    {
                        return TypedResults.Ok("Search saved successfully.");
                    }

                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Save Failed",
                        Detail = "Search save could not be confirmed.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path
                    );
                }
            })
           .WithName("SaveSearch")
           .WithTags("Search")
           .WithSummary("Save user search")
           .WithDescription("Save the search criteria using user ID from frontend.")
           .RequireAuthorization()
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // added save search id
            group.MapPost("/search/by-category-id", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                SaveSearchRequestByIdDto dto,
                IClassifiedService service,
                HttpContext context
            ) =>
            {
                if (dto.UserId == null || dto.UserId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search name is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (dto.SearchQuery == null || string.IsNullOrWhiteSpace(dto.SearchQuery.Text))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search query text is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var success = await service.SaveSearchById(dto);
                    if (success)
                    {
                        return TypedResults.Ok("Search saved successfully.");
                    }

                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Save Failed",
                        Detail = "Search save could not be confirmed.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path
                    );
                }
            })
           .WithName("SaveSearchById")
           .WithTags("Search")
           .WithSummary("Save user searcssh")
           .WithDescription("Save the search criteria using user ID from frontendss.")
           .ExcludeFromDescription()
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // get save search
            group.MapGet("/search/getsavedSearches", async Task<Results<
                Ok<List<SavedSearchResponseDto>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                IClassifiedService service,
                HttpContext context
            ) =>
            {
                Guid? userId = context.User.GetId();
                if (userId == null || userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the query.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                else
                {
                    try
                    {
                        var result = await service.GetSearches(userId.ToString());
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError,
                            instance: context.Request.Path
                        );
                    }
                }
            })
            .WithName("GetSavedSearch")
            .WithTags("Search")
            .WithSummary("Get saved searches")
            .WithDescription("Get all saved searches for the current user.")
            .RequireAuthorization()
            .Produces<List<SavedSearchResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/search/save-by-id", async Task<Results<
                Ok<List<SavedSearchResponseDto>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                [Required][FromQuery] Guid userId,
                IClassifiedService service,
                HttpContext context
            ) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the query.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                else
                {
                    try
                    {
                        var result = await service.GetSearches(userId.ToString());
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError,
                            instance: context.Request.Path
                        );
                    }
                }
            })
            .WithName("GetSavedSearcheById")
            .WithTags("Searchs")
            .WithSummary("Get saved searchess")
            .WithDescription("Get all saved searches for the current users.")
            .ExcludeFromDescription()
            .Produces<List<SavedSearchResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("itemsAd-dashboard", async Task<IResult> (
                HttpContext context,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId(); 

                if (userId == null || userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the JWT token.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetUserItemsAdsWithDashboard(userId, token);

                    if ((result?.ItemsAds.PublishedAds?.Any() != true) &&
                        (result?.ItemsAds.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No ads were found for user ID '{userId}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested user or ads data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetUserItemsAdsWithDashboard")
                .WithTags("Items")
                .WithSummary("Get all user ads and dashboard")
                .WithDescription("Returns both published/unpublished ads and dashboard metrics for a given user ID (from token).")
                .RequireAuthorization() 
                .Produces<ItemAdsAndDashboardResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("itemsAd-dashboard-byId", async Task<IResult> (
                [FromQuery] Guid userId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetUserItemsAdsWithDashboard(userId, token);

                    if ((result?.ItemsAds.PublishedAds?.Any() != true) &&
                        (result?.ItemsAds.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No ads were found for user ID '{userId}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested user or ads data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
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
                .WithName("GetUserItemsAdsWithDashboardById") 
                .WithTags("Items")
                .WithSummary("Get all user ads and dashboard (By Id)")
                .WithDescription("Returns both published/unpublished ads and dashboard metrics for a given user ID (from query).")
                .Produces<ItemAdsAndDashboardResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();



            group.MapGet("prelovedAd-dashboard", async Task<IResult> (
                HttpContext httpContext,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var userId = httpContext.User.GetId(); 
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetUserPrelovedAdsAndDashboard(userId, token);

                    if ((result?.PrelovedAds.PublishedAds?.Any() != true) &&
                        (result?.PrelovedAds.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No Preloved ads were found for authenticated user.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested Preloved ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetUserPrelovedAdsWithDashboard") 
            .WithTags("Preloved")
            .WithSummary("Get all authenticated user's Preloved ads and dashboard")
            .WithDescription("Returns Preloved ads and dashboard for the currently authenticated user.")
            .Produces<PrelovedAdsAndDashboardResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("prelovedAd-dashboard-byId", async Task<IResult> (
                Guid userId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetUserPrelovedAdsAndDashboard(userId, token);

                    if ((result?.PrelovedAds.PublishedAds?.Any() != true) &&
                        (result?.PrelovedAds.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No Preloved ads were found for user ID '{userId}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested Preloved ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetUserPrelovedAdsWithDashboardById") 
            .WithTags("Preloved")
            .WithSummary("Get all user Preloved ads and dashboard (by userId)")
            .WithDescription("Returns Preloved ads and dashboard for a user specified via route parameter.")
            .Produces<PrelovedAdsAndDashboardResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .ExcludeFromDescription();

            // itemsAd post
            group.MapPost("items/post", async Task<IResult> (
                HttpContext httpContext,
                ClassifiedItems dto,
                IClassifiedService service,
                [FromServices]ISearchService svc,
                CancellationToken token) =>
            {
                try
                {
                    var userId = httpContext.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    dto.UserId = userId;
                    var response = await service.CreateClassifiedItemsAd(dto, token);
                    var imageUrls = new List<string>
                    {
                        "https://www.qatarliving.com/_next/image?url=%2Fimages%2Ftwo-iphone.jpeg&w=828&q=75",
                        "https://www.qatarliving.com/_next/image?url=https%3A%2F%2Fwww.qatarliving.com%2Fq%2Fs3%2Ffiles%2Fstyles%2Fvehicle_listing_v3%2Fs3%2Fvehicles%2F2025%2F06%2F14%2F10081967%2FWhatsApp%20Image%202025-06-14%20at%2011.53.41_84673a0e.jpg&w=384&q=75",
                        "https://th.bing.com/th/id/OIP.UCezLikSjxX91hGZNpCZQgHaHa?rs=1&pid=ImgDetMain&cb=idpwebp2&o=7&rm=3",
                        "https://i.pinimg.com/originals/82/32/27/823227eb85d3f43ede612e28e53a9d7c.jpg",
                        "https://www.techspot.com/images2/news/bigimage/2023/11/2023-11-14-image-9.jpg",
                        "https://th.bing.com/th/id/OIP.HgPa0rOyQFm1dCuGRzjc0AHaFj?rs=1&pid=ImgDetMain&cb=idpwebp2&o=7&rm=3"
                    };

                    // Randomly select 1 or more images from the list
                    var random = new Random();
                    var selectedImageUrl = imageUrls[random.Next(imageUrls.Count)];

                    // Create ImageInfo object and add to the list of images
                    var images = new List<ImageInfo>
                    {
                        new ImageInfo
                        {
                            AdImageFileNames = "random_image.jpg", 
                            Url = selectedImageUrl,
                            Order = 0
                        }
                    };
                    var classifiedsIndex = new ClassifiedsIndex
                    {
                        SubVertical = dto.SubVertical,
                        Title = dto.Title,
                        Description = dto.Description,
                        CategoryId = dto.CategoryId.ToString(),
                        Category = dto.Category,
                        L1Category = dto.l1Category,
                        L2Category = dto.L2Category,
                        Price = (double?)dto.Price,
                        PriceType = dto.PriceType,
                        Location = dto.Location.FirstOrDefault(),
                        PhoneNumber = dto.PhoneNumber,
                        WhatsappNumber = dto.WhatsAppNumber,
                        UserId = dto.UserId.ToString(),
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Images = images,
                        Make = dto.MakeType,
                        Model = dto.Model,
                        Brand = dto.Brand,
                        Processor = dto.Processor,
                        Ram = dto.Ram,
                        SizeType = dto.Size,
                        Size = dto.SizeValue,
                        Status = "Published",
                        StreetNumber = dto.StreetNumber,
                        Zone = dto.Zone,
                        Storage = dto.Capacity,
                        BuildingNumber = dto.BuildingNumber,
                        Colour = dto.Color,
                        BatteryPercentage = dto.BatteryPercentage,
                        ExpiryDate = dto.ExpiryDate,
                        RefreshExpiryDate = dto.RefreshExpiry
                    };
                    var indexDocument = new CommonIndexRequest
                    {
                        VerticalName = ConstantValues.Verticals.Classifieds,
                        ClassifiedsItem = classifiedsIndex
                    };
                    var msg = await svc.UploadAsync(indexDocument);
                    return TypedResults.Created($"/api/classifieds/items/user-ads-by-id/{response.AdId}", response);

                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }               
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostItemsAd")
                .WithTags("Classified")
                .WithSummary("Post classified items ad using authenticated user")
                .WithDescription("Takes user ID from JWT token and creates the ad.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("items/post-by-id", async Task<IResult> (
                ClassifiedItems dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var response = await service.CreateClassifiedItemsAd(dto, token);
                    return TypedResults.Created($"/api/classifieds/items/user-ads-by-id/{response.AdId}", response);

                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }                
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostItemsAdById")
                .WithTags("Classified")
                .WithSummary("Post classified items ad using provided UserId")
                .WithDescription("For admin/service scenarios where the UserId is passed explicitly.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();
            
            
            group.MapPost("preloved/post", async Task<IResult> (
                HttpContext httpContext,
                ClassifiedPreloved dto,
                IClassifiedService service,
                [FromServices] ISearchService svc,
                CancellationToken token) =>
            {
                try
                {
                    var userId = httpContext.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    dto.UserId = userId;
                    var result = await service.CreateClassifiedPrelovedAd(dto, token);
                    var prelovedIndex = new ClassifiedsIndex
                    {
                        SubVertical = dto.SubVertical,
                        Title = dto.Title,
                        Description = dto.Description,
                        CategoryId = dto.CategoryId.ToString(),
                        Category = dto.Category,
                        L1Category = dto.l1Category,
                        L2Category = dto.L2Category,
                        Price = (double?)dto.Price,
                        PriceType = dto.PriceType,
                        Location = dto.Location.FirstOrDefault(),
                        PhoneNumber = dto.PhoneNumber,
                        WhatsappNumber = dto.WhatsAppNumber,
                        UserId = dto.UserId.ToString(),
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Images = new List<ImageInfo>(),
                        Status = "Active",
                        Model = dto.Model,
                        Brand = dto.Brand,
                        Processor = dto.Processor,
                        Ram = dto.Ram,
                        SizeType = dto.Size,
                        Size = dto.SizeValue,
                        StreetNumber = dto.StreetNumber,
                        Zone = dto.Zone,
                        Storage = dto.Capacity,
                        BuildingNumber = dto.BuildingNumber,
                        Colour = dto.Color,
                        BatteryPercentage = dto.BatteryPercentage,
                        ExpiryDate = dto.ExpiryDate,
                        RefreshExpiryDate = dto.RefreshExpiry
                    };
                    var indexDocument = new CommonIndexRequest
                    {
                        VerticalName = ConstantValues.Verticals.Classifieds,
                        ClassifiedsItem = prelovedIndex
                    };
                    var msg = await svc.UploadAsync(indexDocument);

                    return TypedResults.Created(
           $"/api/classifieds/preloved/user-ads-by-id/{result.AdId}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }               
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostPrelovedAd")
                .WithTags("Classified")
                .WithSummary("Post classified preloved ad using authenticated user")
                .WithDescription("Takes user ID from JWT token and creates the ad.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("preloved/post-by-id", async Task<IResult> (
                ClassifiedPreloved dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateClassifiedPrelovedAd(dto, token);

                    return TypedResults.Created(
           $"/api/classifieds/preloved/user-ads-by-id/{result.AdId}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostPrelovedAdById")
                .WithTags("Classified")
                .WithSummary("Post classified preloved ad using provided UserId")
                .WithDescription("For admin/service scenarios where the UserId is passed explicitly.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("collectibles/post", async Task<IResult> (
                HttpContext httpContext,
                ClassifiedCollectibles dto,
                IClassifiedService service,
                [FromServices]ISearchService svc,
                CancellationToken token) =>
            {
                try
                {
                    var userId = httpContext.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    dto.UserId = userId;
                    var result = await service.CreateClassifiedCollectiblesAd(dto, token);
                    var collectiblesIndex = new ClassifiedsIndex
                    {
                        SubVertical = dto.SubVertical,
                        Title = dto.Title,
                        Description = dto.Description,
                        CategoryId = dto.CategoryId.ToString(),
                        Category = dto.Category,
                        L1Category = dto.l1Category,
                        L2Category = dto.L2Category,
                        Price = (double?)dto.Price,
                        PriceType = dto.PriceType,
                        Location = dto.Location.FirstOrDefault(),
                        PhoneNumber = dto.PhoneNumber,
                        WhatsappNumber = dto.WhatsAppNumber,
                        UserId = dto.UserId.ToString(),
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Images = new List<ImageInfo>(),
                        YearEra = dto.YearOrEra,
                        Rarity = dto.Rarity,
                        Material = dto.Material,
                        Status = "Active",
                        SerialNumber = dto.SerialNumber,
                        SignedBy = dto.SignedBy,
                        IsSigned = dto.Signed,
                        ExpiryDate = dto.ExpiryDate,
                        RefreshExpiryDate = dto.RefreshExpiry
                    };
                    var indexDocument = new CommonIndexRequest
                    {
                        VerticalName = ConstantValues.Verticals.Classifieds,
                        ClassifiedsItem = collectiblesIndex
                    };
                    var msg = await svc.UploadAsync(indexDocument);


                    return TypedResults.Created(
                        $"/api/classifieds/collectibles/user-ads-by-id/{result.AdId}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }               
                catch (Exception ex)
                {
                    return ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false)
                        ? TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
                            Status = StatusCodes.Status404NotFound
                        })
                        : TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                }
            })
                .WithName("PostCollectiblesAd")
                .WithTags("Classified")
                .WithSummary("Post classified collectibles ad using authenticated user")
                .WithDescription("Takes user ID from JWT token and creates the ad.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("collectibles/post-by-id", async Task<IResult> (
                ClassifiedCollectibles dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateClassifiedCollectiblesAd(dto, token);

                    return TypedResults.Created(
                        $"/api/classifieds/collectibles/user-ads-by-id/{result.AdId}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }               
                catch (Exception ex)
                {
                    return ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false)
                        ? TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
                            Status = StatusCodes.Status404NotFound
                        })
                        : TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                }
            })
                .WithName("PostCollectiblesAdById")
                .WithTags("Classified")
                .WithSummary("Post classified collectibles ad using provided UserId")
                .WithDescription("For admin/service scenarios where the UserId is passed explicitly.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();



            group.MapPost("deals/post", async Task<IResult> (
                HttpContext httpContext,
                ClassifiedDeals dto,
                IClassifiedService service,
                [FromServices]ISearchService svc,
                CancellationToken token) =>
            {
                try
                {
                    var userId = httpContext.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    dto.UserId = userId;
                    var result = await service.CreateClassifiedDealsAd(dto, token);
                    var dealsIndex = new ClassifiedsIndex
                    {
                        SubVertical = dto.SubVertical,
                        Title = dto.Title,
                        Description = dto.Description,
                        Location = dto.Location.FirstOrDefault(),
                        CreatedDate = DateTime.UtcNow,
                        Images = new List<ImageInfo>(),
                        Status = "Active",
                        FlyerFileName = dto.FlyerName,
                        FlyerXmlLink = dto.XMLLink,
                        ExpiryDate = dto.ExpiryDate,
                        RefreshExpiryDate = dto.RefreshExpiry
                    };
                    var indexDocument = new CommonIndexRequest
                    {
                        VerticalName = ConstantValues.Verticals.Classifieds,
                        ClassifiedsItem = dealsIndex
                    };
                    var msg = await svc.UploadAsync(indexDocument);

                    return TypedResults.Created($"/api/classifieds/deals/user-ads-by-id/{result.AdId}", result);

                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostDealsAd")
                .WithTags("Classified")
                .WithSummary("Post classified Deals ad using authenticated user")
                .WithDescription("Takes user ID from JWT token and creates the ad.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("deals/post-by-id", async Task<IResult> (
                ClassifiedDeals dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateClassifiedDealsAd(dto, token);

                    return TypedResults.Created($"/api/classifieds/deals/user-ads-by-id/{result.AdId}", result);

                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }                
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostDealsAdById")
                .WithTags("Classified")
                .WithSummary("Post classified deals ad using provided UserId")
                .WithDescription("For admin/service scenarios where the UserId is passed explicitly.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)                
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();


            group.MapGet("/collectibles", async Task<Results<
               Ok<CollectiblesResponse>,
               BadRequest<ProblemDetails>,
               UnauthorizedHttpResult,
               ProblemHttpResult>>
            (
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
            ) =>
            {
                Guid? userId = context.User.GetId();

                if (userId == null || userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the token.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var result = await service.GetCollectibles(userId.ToString(), cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (FileNotFoundException fileEx)
                {
                    return TypedResults.Problem(
                        title: "File Not Found",
                        detail: fileEx.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        instance: context.Request.Path);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("GetCollectibles")
            .WithTags("Collectibles")
            .WithSummary("Get collectibles for the logged-in user")
            .WithDescription("Returns collectibles data for the current user based on token.")
            .Produces<CollectiblesResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

            group.MapGet("/collectibles-by-id", async Task<Results<
                  Ok<CollectiblesResponse>,
                  BadRequest<ProblemDetails>,
                  ProblemHttpResult>>
              (
                  [Required][FromQuery] Guid userId,
                  IClassifiedService service,
                  HttpContext context,
                  CancellationToken cancellationToken
              ) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the query.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var result = await service.GetCollectibles(userId.ToString(), cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (FileNotFoundException fileEx)
                {
                    return TypedResults.Problem(
                        title: "File Not Found",
                        detail: fileEx.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        instance: context.Request.Path);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("GetCollectiblesById")
            .WithTags("Collectibles")
            .WithSummary("Get collectibles for a specified user ID")
            .WithDescription("Returns collectibles data for a given user ID passed as query parameter.")
            .Produces<CollectiblesResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .ExcludeFromDescription()
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/items-ad/{adId:guid}", async Task<Results<
                Ok<DeleteAdResponseDto>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                Guid adId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (adId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid non-empty GUID.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var response = await service.DeleteClassifiedItemsAd(adId, cancellationToken);
                    return TypedResults.Ok(response);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Ad Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound,
                        Instance = context.Request.Path
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested classified item ad not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("DeleteClassifiedItemsAd")
                .WithTags("Classified")
                .WithSummary("Delete a classified items ad by ID")
                .WithDescription("Deletes a classified items ad using the provided Ad ID. Ad must exist in Dapr state store.")
                .Produces<DeleteAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/preloved-ad/{adId:guid}", async Task<Results<
                Ok<DeleteAdResponseDto>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                Guid adId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (adId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid non-empty GUID.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var response = await service.DeleteClassifiedPrelovedAd(adId, cancellationToken);
                    return TypedResults.Ok(response);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Ad Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound,
                        Instance = context.Request.Path
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested classified preloved ad not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("DeleteClassifiedPrelovedAd")
                .WithTags("Classified")
                .WithSummary("Delete a classified preloved ad by ID")
                .WithDescription("Deletes a classified preloved ad using the provided Ad ID. Ad must exist in Dapr state store.")
                .Produces<DeleteAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/collectibles-ad/{adId:guid}", async Task<Results<
                Ok<DeleteAdResponseDto>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                Guid adId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (adId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid non-empty GUID.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var response = await service.DeleteClassifiedCollectiblesAd(adId, cancellationToken);
                    return TypedResults.Ok(response);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Ad Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound,
                        Instance = context.Request.Path
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested classified collectibles ad not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("DeleteClassifiedCollectiblesAd")
                .WithTags("Classified")
                .WithSummary("Delete a classified collectibles ad by ID")
                .WithDescription("Deletes a classified collectibles ad using the provided Ad ID. The ad must exist in Dapr state store.")
                .Produces<DeleteAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapDelete("/deals-ad/{adId:guid}", async Task<Results<
                Ok<DeleteAdResponseDto>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                Guid adId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (adId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid non-empty GUID.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var response = await service.DeleteClassifiedDealsAd(adId, cancellationToken);
                    return TypedResults.Ok(response);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Ad Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound,
                        Instance = context.Request.Path
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested classified deals ad not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("DeleteClassifiedDealsAd")
                .WithTags("Classified")
                .WithSummary("Delete a classified deals ad by ID")
                .WithDescription("Deletes a classified deals ad using the provided Ad ID. Ad must exist in Dapr state store.")
                .Produces<DeleteAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("items/user-ads/published", async Task<IResult> (
                HttpContext context,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetUserPublishedItemsAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }                
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested published ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetUserPublishedItemsAds")
                .WithTags("Classified")
                .WithSummary("Get published classified items ads for current user")
                .WithDescription("Retrieves only published item ads for the authenticated user with pagination.")
                .Produces<PaginatedAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();


            group.MapGet("items/user-ads-by-id/{userId:guid}/published", async Task<IResult> (
                Guid userId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetUserPublishedItemsAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }               
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested published ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetUserPublishedItemsAdsById")
                .WithTags("Classified")
                .WithSummary("Get published classified items ads by user ID")
                .WithDescription("Retrieves only published item ads for the specified user with pagination (admin or service use).")
                .Produces<PaginatedAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("items/user-ads/unpublished", async Task<IResult> (
                HttpContext context,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetUserUnPublishedItemsAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }              
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested unpublished ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetUserUnPublishedItemsAds")
                .WithTags("Classified")
                .WithSummary("Get unpublished classified items ads for current user")
                .WithDescription("Retrieves only unpublished item ads for the authenticated user with optional pagination.")
                .Produces<PaginatedAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("items/user-ads-by-id/{userId:guid}/unpublished", async Task<IResult> (
                Guid userId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IClassifiedService service,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetUserUnPublishedItemsAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }               
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested unpublished ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,                            
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetUserUnPublishedItemsAdsById")
                .WithTags("Classified")
                .WithSummary("Get unpublished classified items ads by user ID")
                .WithDescription("Retrieves only unpublished item ads for the specified user (for admin or service use) with pagination.")
                .Produces<PaginatedAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();



            group.MapGet("preloved/user-ads/published",async Task<IResult> (
                HttpContext context,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = "Authenticated user ID is missing or invalid.",
                    Status = StatusCodes.Status400BadRequest
                });
                
                try
                {
                    var result = await service.GetUserPublishedPrelovedAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }               
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested published ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });
                    
                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserPublishedPrelovedAds")
                .WithTags("Classified")
                .WithSummary("Get published/approved preloved ads for current user")
                .WithDescription("Retrieves only published or approved Preloved ads for the authenticated user with optional pagination.")
                .Produces<PaginatedPrelovedAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("preloved/user-ads-by-id/{userId:guid}/published", async Task<IResult> (
                Guid userId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });

                try
                {
                    var result = await service.GetUserPublishedPrelovedAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }                                              
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested published ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,
                        });
                    
                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,                            
                        });
                    
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserPublishedPrelovedAdsById")
                .WithTags("Classified")
                .WithSummary("Get published/approved preloved ads by user ID")
                .WithDescription("Retrieves published or approved Preloved ads for the specified user ID (admin/service use) with optional pagination.")
                .Produces<PaginatedPrelovedAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("preloved/user-ads/unpublished", async Task<IResult> (
                HttpContext context,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });
                try
                {
                    var result = await service.GetUserUnPublishedPrelovedAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }               
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested unpublished ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });
                    
                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                
                }
            })
                .WithName("GetUserUnPublishedPrelovedAds")
                .WithTags("Classified")
                .WithSummary("Get unpublished preloved ads for current user")
                .WithDescription("Retrieves only unpublished Preloved ads for the authenticated user with optional pagination.")
                .Produces<PaginatedPrelovedAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();
            
            group.MapGet("preloved/user-ads-by-id/{userId:guid}/unpublished", async Task<IResult> (
                Guid userId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {                
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });
                try
                {
                    var result = await service.GetUserUnPublishedPrelovedAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }                
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested unpublished ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,                            
                        });
                    
                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                        });
                    
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                
                }
            })
                .WithName("GetUserUnPublishedPrelovedAdsById")
                .WithTags("Classified")
                .WithSummary("Get unpublished preloved ads by user ID")
                .WithDescription("Retrieves unpublished Preloved ads for the specified user ID (admin/service use) with optional pagination.")
                .Produces<PaginatedPrelovedAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();


            group.MapGet("collectibles/user-ads/published", async Task<IResult> (
                HttpContext context,
                [FromQuery] int? page,
                [FromQuery] int? pageSize, 
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });

                try
                {
                    var result = await service.GetUserPublishedCollectiblesAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }              
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested published ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });

                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserPublishedCollectiblesAds")
                .WithTags("Classified")
                .WithSummary("Get published collectibles ads for current user")
                .WithDescription("Retrieves only published or approved Collectibles ads for the authenticated user with pagination.")
                .Produces<PaginatedCollectiblesAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();


            group.MapGet("collectibles/user-ads-by-id/{userId:guid}/published", async Task<IResult> (
                Guid userId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });

                try
                {
                    var result = await service.GetUserPublishedCollectiblesAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }                
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested published ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });

                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserPublishedCollectiblesAdsById")
                .WithTags("Classified")
                .WithSummary("Get published collectibles ads by user ID")
                .WithDescription("Retrieves only published or approved Collectibles ads for the specified user with pagination (admin/service use).")
                .Produces<PaginatedCollectiblesAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("collectibles/user-ads/unpublished", async Task<IResult> (
                HttpContext context,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });

                try
                {
                    var result = await service.GetUserUnPublishedCollectiblesAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }               
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested unpublished ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });

                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserUnPublishedCollectiblesAds")
                .WithTags("Classified")
                .WithSummary("Get unpublished collectibles ads for current user")
                .WithDescription("Retrieves only unpublished Collectibles ads for the authenticated user with pagination.")
                .Produces<PaginatedCollectiblesAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)                
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("collectibles/user-ads-by-id/{userId:guid}/unpublished", async Task<IResult> (
                Guid userId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });

                try
                {
                    var result = await service.GetUserUnPublishedCollectiblesAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }              
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested unpublished ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });

                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserUnPublishedCollectiblesAdsById")
                .WithTags("Classified")
                .WithSummary("Get unpublished collectibles ads by user ID")
                .WithDescription("Retrieves unpublished Collectibles ads for the specified user with pagination (admin/service use).")
                .Produces<PaginatedCollectiblesAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)               
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();


            group.MapGet("deals/user-ads/published", async Task<IResult> (
                HttpContext context,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });

                try
                {
                    var result = await service.GetUserPublishedDealsAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested published ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });

                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserPublishedDealsAds")
                .WithTags("Classified")
                .WithSummary("Get published deals ads for current user")
                .WithDescription("Retrieves only published or approved Deals ads for the authenticated user with pagination.")
                .Produces<PaginatedDealsAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("deals/user-ads-by-id/{userId:guid}/published", async Task<IResult> (
                Guid userId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });

                try
                {
                    var result = await service.GetUserPublishedDealsAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }               
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested published ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });

                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserPublishedDealsAdsById")
                .WithTags("Classified")
                .WithSummary("Get published deals ads by user ID")
                .WithDescription("Retrieves only published or approved Deals ads for the specified user with pagination (admin or service use).")
                .Produces<PaginatedDealsAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("deals/user-ads/unpublished", async Task<IResult> (
                HttpContext context,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });

                try
                {
                    var result = await service.GetUserUnPublishedDealsAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }               
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested unpublished ads or user data not found.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = context.Request.Path
                        });

                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserUnPublishedDealsAds")
                .WithTags("Classified")
                .WithSummary("Get unpublished deals ads for current user")
                .WithDescription("Retrieves only unpublished Deals ads for the authenticated user with pagination.")
                .Produces<PaginatedDealsAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();


            group.MapGet("deals/user-ads-by-id/{userId:guid}/unpublished", async Task<IResult> (
                Guid userId,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] AdSortOption? sortOption,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });

                try
                {
                    var result = await service.GetUserUnPublishedDealsAds(userId, page, pageSize, sortOption, search, token);
                    return TypedResults.Ok(result);
                }                
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested unpublished ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });

                    if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetUserUnPublishedDealsAdsById")
                .WithTags("Classified")
                .WithSummary("Get unpublished deals ads by user ID")
                .WithDescription("Retrieves unpublished Deals ads for the specified user with pagination (admin or service use).")
                .Produces<PaginatedDealsAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("/category", async Task<IResult> (
                CategoryDtos dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Category name must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Vertical))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Vertical must be specified (e.g., items, preloved, collectibles, deals).",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                try
                {
                    var id = await service.CreateCategory(dto, token);
                    return TypedResults.Ok(id);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Category Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "One or more required resources were not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("CreateCategory")
                .WithTags("Classified")
                .WithSummary("Create a new category with optional fields")
                .WithDescription("Creates a new parent or child category in the specified vertical (items, preloved, collectibles, deals) with optional dynamic fields.")
                .Produces<Guid>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/category/{vertical}/{parentId:guid}", async Task<IResult> (
                string vertical,
                Guid parentId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(vertical))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Vertical must be specified.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                if (parentId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Parent category ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetChildCategories(vertical, parentId, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Category Retrieval Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "No child categories found for the given parent ID.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetChildCategories")
                .WithTags("Classified")
                .WithSummary("Get child categories of a given parent category for a specific vertical")
                .WithDescription("Retrieves a list of subcategories under the specified parent category for the given vertical (e.g., items, preloved, collectibles).")
                .Produces<List<Categories>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            group.MapGet("/category/tree/{vertical}/{categoryId:guid}", async Task<IResult> (
                string vertical,
                Guid categoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(vertical))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Vertical must be specified (e.g., items, preloved, collectibles, deals).",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                if (categoryId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Category ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var tree = await service.GetCategoryTree(vertical, categoryId, token);

                    if (tree == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Category Not Found",
                            Detail = $"No category tree found for ID {categoryId} in vertical '{vertical}'",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(tree);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Category Tree Retrieval Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested category or tree not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetCategoryHierarchyTree")
                .WithTags("Classified")
                .WithSummary("Returns full recursive category tree for a given vertical")
                .WithDescription("Fetches the entire nested hierarchy of a given category and its child categories within the specified vertical (items, preloved, etc).")
                .Produces<CategoryTreeDto>(StatusCodes.Status200OK)
                .Produces<CategoryTreeDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapDelete("/category/{vertical}/{categoryId:guid}/tree", async Task<IResult> (
                string vertical,
                Guid categoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(vertical))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Vertical must be specified (e.g., items, preloved, collectibles, deals).",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                if (categoryId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Category ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    await service.DeleteCategoryTree(vertical, categoryId, token);

                    return TypedResults.Ok(new
                    {
                        Message = $"Category tree {categoryId} deleted successfully from vertical '{vertical}'."
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Delete Operation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Category or child nodes not found for deletion.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("DeleteCategoryTree")
                .WithTags("Classified")
                .WithSummary("Deletes a category and all of its child categories recursively from a given vertical.")
                .WithDescription("Performs recursive deletion of a category and all nested children within the specified vertical (items, preloved, etc).")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/category/{vertical}/all-trees", async Task<IResult> (
                string vertical,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(vertical))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Vertical must be specified (e.g., items, preloved, collectibles, deals).",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetAllCategoryTrees(vertical, token);

                    if (result == null || result.Count == 0)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Categories Found",
                            Detail = $"No root categories or hierarchies were found for vertical '{vertical}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Category hierarchy not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetAllCategoryTrees")
                .WithTags("Classified")
                .WithSummary("Returns all root categories and their full hierarchy for a specific vertical")
                .WithDescription("Fetches all top-level categories and recursively includes all nested subcategories and fields for the specified vertical (items, preloved, collectibles, deals).")
                .Produces<List<CategoryTreeDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("items/user-ads/unpublish", async Task<IResult> (
                HttpContext context,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or does not match request.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkUnpublishItemsAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Unpublish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkUnpublishItemsAds")
                .WithTags("Classified")
                .WithSummary("Unpublish multiple published item ads")
                .WithDescription("Unpublishes selected published item ads for the authenticated user.")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("items/user-ads-by-id/{userId:guid}/unpublish", async Task<IResult> (
                 Guid userId,
                 List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is missing or does not match payload.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkUnpublishItemsAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Unpublish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkUnpublishItemsAdsById")
                .WithTags("Classified")
                .WithSummary("Unpublish multiple published item ads by user ID")
                .WithDescription("Unpublishes selected published item ads for a specific user (admin use only).")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("items/user-ads/publish", async Task<IResult> (
                HttpContext context,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or does not match request.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkPublishItemsAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Publish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkPublishItemsAds")
                .WithTags("Classified")
                .WithSummary("Publish multiple unpublished item ads")
                .WithDescription("Publishes selected unpublished item ads for the authenticated user.")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("items/user-ads-by-id/{userId:guid}/publish", async Task<IResult> (
                Guid userId,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is missing or does not match payload.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkPublishItemsAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Publish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkPublishItemsAdsById")
                .WithTags("Classified")
                .WithSummary("Publish multiple unpublished item ads by user ID")
                .WithDescription("Publishes selected unpublished item ads for a specific user (admin use only).")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("/preloved/user-ads/publish", async Task<IResult> (
                HttpContext context,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or does not match request.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkPublishPrelovedAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Publish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkPublishPrelovedAds")
                .WithTags("Classified")
                .WithSummary("Publish multiple preloved ads")
                .WithDescription("Publishes draft/unpublished preloved ads for the authenticated user.")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("/preloved/user-ads-by-id/{userId:guid}/publish", async Task<IResult> (
                Guid userId,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkPublishPrelovedAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Publish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkPublishPrelovedAdsById")
                .WithTags("Classified")
                .WithSummary("Publish multiple preloved ads by user ID")
                .WithDescription("Publishes selected unpublished preloved ads for a specific user (admin use).")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("/preloved/user-ads/unpublish", async Task<IResult> (
                HttpContext context,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or does not match request.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkUnpublishPrelovedAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Unpublish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkUnpublishPrelovedAds")
                .WithTags("Classified")
                .WithSummary("Unpublish multiple preloved ads")
                .WithDescription("Unpublishes active preloved ads for the authenticated user.")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("/preloved/user-ads-by-id/{userId:guid}/unpublish", async Task<IResult> (
                Guid userId,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkUnpublishPrelovedAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Unpublish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkUnpublishPrelovedAdsById")
                .WithTags("Classified")
                .WithSummary("Unpublish multiple preloved ads by user ID")
                .WithDescription("Unpublishes published preloved ads for a specific user (admin use).")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("/deals/user-ads/publish", async Task<IResult> (
                HttpContext context,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or does not match request.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkPublishDealsAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Publish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkPublishDealsAds")
                .WithTags("Classified")
                .WithSummary("Publish multiple deals ads")
                .WithDescription("Publishes draft/unpublished deals ads for the authenticated user.")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("/deals/user-ads-by-id/{userId:guid}/publish", async Task<IResult> (
                Guid userId,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkPublishDealsAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Publish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkPublishDealsAdsById")
                .WithTags("Classified")
                .WithSummary("Publish multiple deals ads by user ID")
                .WithDescription("Publishes selected unpublished deals ads for a specific user (admin use).")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("/deals/user-ads/unpublish", async Task<IResult> (
                HttpContext context,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or invalid.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkUnpublishDealsAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Unpublish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkUnpublishDealsAds")
                .WithTags("Classified")
                .WithSummary("Unpublish multiple deals ads")
                .WithDescription("Unpublishes published deals ads for the authenticated user.")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("/deals/user-ads-by-id/{userId:guid}/unpublish", async Task<IResult> (
                Guid userId,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkUnpublishDealsAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Unpublish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkUnpublishDealsAdsById")
                .WithTags("Classified")
                .WithSummary("Unpublish multiple deals ads by user ID")
                .WithDescription("Unpublishes selected published deals ads for a specific user (admin use).")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("/collectibles/user-ads/publish", async Task<IResult> (
                HttpContext context,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or does not match request.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkPublishCollectiblesAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Publish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkPublishCollectiblesAds")
                .WithTags("Classified")
                .WithSummary("Publish multiple collectibles ads")
                .WithDescription("Publishes draft/unpublished collectibles ads for the authenticated user.")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("/collectibles/user-ads-by-id/{userId:guid}/publish", async Task<IResult> (
                Guid userId,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkPublishCollectiblesAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Publish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkPublishCollectiblesAdsById")
                .WithTags("Classified")
                .WithSummary("Publish multiple collectibles ads by user ID")
                .WithDescription("Publishes selected unpublished collectibles ads for a specific user (admin use).")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("/collectibles/user-ads/unpublish", async Task<IResult> (
                HttpContext context,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId();
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Authenticated user ID is missing or does not match request.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkUnpublishCollectiblesAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Unpublish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkUnpublishCollectiblesAds")
                .WithTags("Classified")
                .WithSummary("Unpublish multiple collectibles ads")
                .WithDescription("Unpublishes published collectibles ads for the authenticated user.")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("/collectibles/user-ads-by-id/{userId:guid}/unpublish", async Task<IResult> (
                Guid userId,
                List<Guid> adIds,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "User ID is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.BulkUnpublishCollectiblesAds(userId, adIds, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Unpublish Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("BulkUnpublishCollectiblesAdsById")
                .WithTags("Classified")
                .WithSummary("Unpublish multiple collectibles ads by user ID")
                .WithDescription("Unpublishes selected published collectibles ads for a specific user (admin use).")
                .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();


            group.MapGet("/category/{vertical}/{mainCategoryId:guid}/filters", async Task<IResult> (
                string vertical,
                Guid mainCategoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(vertical))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Vertical must be specified.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                if (mainCategoryId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Main category ID must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetFiltersByMainCategoryAsync(vertical, mainCategoryId, token);

                    if (result == null || !result.Any())
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Filters Found",
                            Detail = "No filters were found under the provided main category.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Filter Retrieval Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
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
   .WithName("GetFiltersByMainCategory")
   .WithTags("Classified")
   .WithSummary("Get all filters for a main category")
   .WithDescription("Returns all filter definitions (fields with options) for a given vertical and main category, including inherited fields from subcategories.")
   .Produces<List<CategoryField>>(StatusCodes.Status200OK)
   .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
   .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
   .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
   .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);




            return group;
        }



        public static RouteGroupBuilder MapClassifiedsFeaturedItemEndpoint(this RouteGroupBuilder group)
        {

            group.MapGet("/featured-items", async Task<IResult> (
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
                var searchReq = new CommonSearchRequest
                {
                    Filters = new Dictionary<string, object>
                   {
                        { "IsFeatured",   true },
                        { "SubVertical", "Items" }
                    }
                };

                try
                {
                    CommonSearchResponse response = await searchSvc.SearchAsync(
                        ConstantValues.Verticals.Classifieds,
                        searchReq
                    );

                    var list = response.ClassifiedsItems ?? new List<ClassifiedsIndex>();

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
            .WithName($"GetFeatured_{ConstantValues.Verticals.Classifieds}_Items")
            .WithTags("Classified")
            .WithSummary("Get all featured classified items")
            .WithDescription("Fetches every ClassifiedsIndex document where IsFeatured = true.")
            .Produces<IEnumerable<ClassifiedsIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


           

            return group;
        }

    }
}
