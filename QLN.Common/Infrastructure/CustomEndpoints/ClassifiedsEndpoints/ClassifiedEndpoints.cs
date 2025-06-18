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
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Subscriber" })
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
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Subscriber" })
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

            group.MapGet("items/user-ads", async Task<IResult> (
                HttpContext context,
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
                    var result = await service.GetUserItemsAd(userId, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Data Retrieval Failed",
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
                            Detail = "Requested item ads or user data not found.",
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
                .WithName("GetUserItemsAds")
                .WithTags("Classified")
                .WithSummary("Get classified items ads for current user")
                .WithDescription("Retrieves published and unpublished item ads for the authenticated user from Dapr state store.")
                .Produces<ItemAdListDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("items/user-ads-by-id/{userId:guid}", async Task<IResult> (
                Guid userId,
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
                    var result = await service.GetUserItemsAd(userId, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Data Retrieval Failed",
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
                            Detail = "Requested item ads or user data not found.",
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
                .WithName("GetUserItemsAdsById")
                .WithTags("Classified")
                .WithSummary("Get classified items ads by provided user ID")
                .WithDescription("Retrieves published and unpublished item ads for the specified user ID (for admin or service use).")
                .Produces<ItemAdListDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("preloved/user-ads", async Task<IResult> (
                HttpContext context,
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
                    var result = await service.GetUserPrelovedAds(userId, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Data Retrieval Failed",
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
                            Detail = "Requested preloved ads or user data not found.",
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
                .WithName("GetUserPrelovedAds")
                .WithTags("Classified")
                .WithSummary("Get classified preloved ads for current user")
                .WithDescription("Retrieves published and unpublished preloved ads for the authenticated user from Dapr state store.")
                .Produces<PrelovedAdListDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("preloved/user-ads-by-id/{userId:guid}", async Task<IResult> (
                Guid userId,
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
                    var result = await service.GetUserPrelovedAds(userId, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Data Retrieval Failed",
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
                            Detail = "Requested preloved ads or user data not found.",
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
                .WithName("GetUserPrelovedAdsById")
                .WithTags("Classified")
                .WithSummary("Get classified preloved ads by provided user ID")
                .WithDescription("Retrieves published and unpublished preloved ads for the specified user ID (admin/service use).")
                .Produces<PrelovedAdListDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("collectibles/user-ads", async Task<IResult> (
                HttpContext context,
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
                    var result = await service.GetUserCollectiblesAds(userId, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Data Retrieval Failed",
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
                            Detail = "Requested collectibles ads or user data not found.",
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
                .WithName("GetUserCollectiblesAds")
                .WithTags("Classified")
                .WithSummary("Get classified collectibles ads for current user")
                .WithDescription("Retrieves published and unpublished collectibles ads for the authenticated user from Dapr state store.")
                .Produces<CollectiblesAdListDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();


            group.MapGet("collectibles/user-ads-by-id/{userId:guid}", async Task<IResult> (
                Guid userId,
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
                    var result = await service.GetUserCollectiblesAds(userId, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Data Retrieval Failed",
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
                            Detail = "Requested collectibles ads or user data not found.",
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
                .WithName("GetUserCollectiblesAdsById")
                .WithTags("Classified")
                .WithSummary("Get classified collectibles ads by provided user ID")
                .WithDescription("Retrieves published and unpublished collectibles ads for the specified user ID (admin/service use).")
                .Produces<CollectiblesAdListDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();


            group.MapGet("deals/user-ads", async Task<IResult> (
                HttpContext context,
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
                    var result = await service.GetUserDealsAds(userId, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Data Retrieval Failed",
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
                            Detail = "Requested deals ads or user data not found.",
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
                .WithName("GetUserDealsAds")
                .WithTags("Classified")
                .WithSummary("Get classified deals ads for current user")
                .WithDescription("Retrieves published and unpublished deals ads for the authenticated user from Dapr state store.")
                .Produces<DealsAdListDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("deals/user-ads-by-id/{userId:guid}", async Task<IResult> (
                Guid userId,
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
                    var result = await service.GetUserDealsAds(userId, token);
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Data Retrieval Failed",
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
                            Detail = "Requested deals ads or user data not found.",
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
                .WithName("GetUserDealsAdsById")
                .WithTags("Classified")
                .WithSummary("Get classified deals ads by provided user ID")
                .WithDescription("Retrieves published and unpublished deals ads for the specified user ID (admin/service use).")
                .Produces<DealsAdListDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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
                .WithDescription("Creates a new parent or child category with dynamic fields (used in classified ad creation)")
                .Produces<Guid>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/category/{parentId:guid}", async Task<IResult> (
                Guid parentId,
                IClassifiedService service,
                CancellationToken token) =>
            {
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
                    var result = await service.GetChildCategories(parentId, token);
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
                .WithSummary("Get child categories of a given parent category")
                .WithDescription("Retrieves a list of subcategories directly under the specified parent category.")
                .Produces<List<Categories>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            group.MapGet("/category/tree/{categoryId:guid}", async Task<IResult> (
                Guid categoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
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
                    var tree = await service.GetCategoryTree(categoryId, token);

                    if (tree == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Category Not Found",
                            Detail = $"No category found for ID {categoryId}",
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
                .WithSummary("Returns full recursive category tree")
                .WithDescription("Fetches the entire nested hierarchy of a given category and its child categories.")
                .Produces<CategoryTreeDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapDelete("/category/{categoryId:guid}/tree", async Task<IResult> (
                Guid categoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
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
                    await service.DeleteCategoryTree(categoryId, token);

                    return TypedResults.Ok(new
                    {
                        Message = $"Category tree {categoryId} deleted successfully."
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
                .WithSummary("Deletes a category and all of its child categories recursively.")
                .WithDescription("Performs recursive deletion of a category and all nested children.")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/category/all-trees", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllCategoryTrees(token);

                    if (result == null || result.Count == 0)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Categories Found",
                            Detail = "No root categories or hierarchies were found.",
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
                .WithSummary("Returns all root categories with their full hierarchy")
                .WithDescription("Fetches all top-level categories and recursively includes all nested subcategories and fields.")
                .Produces<List<CategoryTreeDto>>(StatusCodes.Status200OK)
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
                    Top = 100,
                    Filters = new Dictionary<string, object>
                   {
                        { "IsFeaturedItem",   true },
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
