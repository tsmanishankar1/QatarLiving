using Amazon.S3.Model;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Classifieds;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsFo;
using QLN.Common.Infrastructure.Auditlog;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text.Json;
using static QLN.Common.DTO_s.ClassifiedsIndex;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class ClassifiedEndpoints
    {
        const string ModuleName = "Classifieds";
        public static RouteGroupBuilder MapClassifiedEndpoints(this RouteGroupBuilder group)
        {


            group.MapPost("/classifieds/search", async (
                [FromBody] ClassifiedsSearchRequest req,
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
                        Instance = $"/api/classifieds/search"
                    });
                }

                string indexName = req.SubVertical.ToLower() switch
                {
                    "items" => ConstantValues.IndexNames.ClassifiedsItemsIndex,
                    "preloved" => ConstantValues.IndexNames.ClassifiedsPrelovedIndex,
                    "collectibles" => ConstantValues.IndexNames.ClassifiedsCollectiblesIndex,
                    "stores" => ConstantValues.IndexNames.ClassifiedStoresIndex,
                    "deals" => ConstantValues.IndexNames.ClassifiedsDealsIndex,
                    _ => null
                };
                var request = new CommonSearchRequest
                {
                    Text = req.Text,
                    Filters = req.Filters,
                    OrderBy = req.OrderBy,
                    PageNumber = req.PageNumber,
                    PageSize = req.PageSize
                };
                if (indexName == null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid SubVertical",
                        Detail = $"Unsupported subVertical value: '{req.SubVertical}'",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/classifieds/search"
                    });
                }

                try
                {
                    var results = await svc.SearchAsync(indexName, request);
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
                        Instance = $"/api/classifieds/search"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception during search");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classifieds/search"
                    );
                }
            })
            .WithName("SearchClassifiedsUnified")
            .WithTags("Classified")
            .WithSummary("Unified classifieds search by subVertical")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapGet("/classifieds/details/{subVertical}/{slug}", async (
                [FromRoute] string subVertical,
                [FromRoute] string slug,
                [FromQuery] int similarPageSize,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");

                if (string.IsNullOrWhiteSpace(slug))
                {
                    logger.LogWarning("GetDetails called with empty id");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/classifieds/details/{subVertical}/{slug}"
                    });
                }

                try
                {
                    var (indexName, modelType) = subVertical.ToLower() switch
                    {
                        "items" => (ConstantValues.IndexNames.ClassifiedsItemsIndex, typeof(ClassifiedsItemsIndex)),
                        "preloved" => (ConstantValues.IndexNames.ClassifiedsPrelovedIndex, typeof(ClassifiedsPrelovedIndex)),
                        "collectibles" => (ConstantValues.IndexNames.ClassifiedsCollectiblesIndex, typeof(ClassifiedsCollectiblesIndex)),
                        "deals" => (ConstantValues.IndexNames.ClassifiedsDealsIndex, typeof(ClassifiedsDealsIndex)),
                        "stores" => (ConstantValues.IndexNames.ClassifiedStoresIndex, typeof(ClassifiedStoresIndex)),
                        _ => (null, null)
                    };

                    if (indexName == null || modelType == null)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid SubVertical",
                            Detail = $"Unsupported subVertical: '{subVertical}'",
                            Status = StatusCodes.Status400BadRequest,
                            Instance = $"/api/classifieds/details/{subVertical}/{slug}"
                        });
                    }

                    var method = typeof(ISearchService)
                        .GetMethod("GetBySlugWithSimilarAsync")?
                        .MakeGenericMethod(modelType);

                    if (method == null)
                        throw new InvalidOperationException("Method resolution failed");

                    var task = (Task)method.Invoke(svc, new object[] { indexName, slug, similarPageSize });
                    await task.ConfigureAwait(false);

                    var resultProperty = task.GetType().GetProperty("Result");
                    var result = resultProperty?.GetValue(task);

                    return Results.Ok(result);
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = $"No document '{slug}' in '{subVertical}'.",
                        Status = StatusCodes.Status404NotFound,
                        Instance = $"/api/classifieds/details/{subVertical}/{slug}"
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
                        Instance = $"/api/classifieds/details/{subVertical}/{slug}"
                    });
                }
                catch (RequestFailedException ex)
                {
                    logger.LogError(ex, "Azure Search error on details");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway,
                        instance: $"/api/classifieds/details/{subVertical}/{slug}"
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error on details");
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classifieds/details/{subVertical}/{slug}"
                    );
                }
            })
            .WithName("GetClassifiedWithSimilarUnified")
            .WithTags("Classified")
            .WithSummary("Get a classified document with similar entries")
            .WithDescription("Returns a specific classified entry along with similar ones based on category.");

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
                var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var userId = userData.GetProperty("uid").GetString();
                var name = userData.GetProperty("name").GetString();
                if (userId == null)
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
                if (dto.UserId == null)
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

            // sangeeth

            group.MapPost("/savesearchvertical", async Task<Results<
    Ok<string>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>> (
    SaveSearchRequestDto dto,
    IClassifiedService service,
    HttpContext context
) =>
            {
                // Extract userId from the claims
                var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                if (string.IsNullOrEmpty(userClaim))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the token.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var userId = userData.GetProperty("uid").GetString();
                var name = userData.GetProperty("name").GetString();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                // Validate the request body (Search name and SearchQuery)
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
                    var success = await service.SaveSearchByVertical(dto, userId);
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
.WithName("savesearchvertical")
.WithTags("Search")
.WithSummary("Save user search")
.WithDescription("Save the search criteria using user ID from frontend.")
.RequireAuthorization()
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/savesearchinternal", async Task<Results<
     Ok<string>,
     BadRequest<ProblemDetails>,
     ProblemHttpResult>> (
     SaveSearchRequestByIdDto dto, // Use the ByIdDto for internal calls
     IClassifiedService service,
     HttpContext context
 ) =>
            {
                try
                {
                    // Convert SaveSearchRequestByIdDto to SaveSearchRequestDto for the service
                    var serviceDto = new SaveSearchRequestDto
                    {
                        Name = dto.Name,
                        UserId = dto.UserId,
                        CreatedAt = dto.CreatedAt,
                        SearchQuery = dto.SearchQuery,
                        Vertical = dto.Vertical,
                        SubVertical = dto.SubVertical
                    };

                    var success = await service.SaveSearchByVertical(serviceDto, dto.UserId);
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
 .WithName("savesearchinternal")
 .WithTags("Search")
 .WithSummary("Internal endpoint to save search to database")
 .WithDescription("Internal endpoint called by external services to save search data.")
 .ExcludeFromDescription()  // Hide from public API documentation
 .Produces<string>(StatusCodes.Status200OK)
 .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
 .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            group.MapGet("/search/getsavedSearches", async Task<Results<
    Ok<List<SavedSearchResponseDto>>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    [Required][FromQuery] Vertical vertical,
    [FromQuery] SubVertical? subVertical,
    IClassifiedService service,
    HttpContext context
) =>
            {
                var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                if (string.IsNullOrEmpty(userClaim))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Authentication Error",
                        Detail = "User claim not found in token.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var userId = userData.GetProperty("uid").GetString();

                    if (string.IsNullOrEmpty(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Valid User ID must be provided in the token.",
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }

                    // Inline SubVertical validation
                    if (subVertical.HasValue)
                    {
                        var isValid = vertical switch
                        {
                            Vertical.Vehicles => subVertical.Value is SubVertical.Items or SubVertical.Deals or SubVertical.Preloved,
                            Vertical.Properties => subVertical.Value is SubVertical.Items or SubVertical.Deals,
                            Vertical.Rewards => subVertical.Value is SubVertical.Deals or SubVertical.Stores,
                            Vertical.Classifieds => subVertical.Value is SubVertical.Items or SubVertical.Preloved or SubVertical.Collectibles,
                            Vertical.Services => subVertical.Value is SubVertical.Services,
                            Vertical.Content => subVertical.Value is SubVertical.News or SubVertical.Daily or SubVertical.Events or SubVertical.Community,
                            _ => false
                        };

                        if (!isValid)
                        {
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Validation Error",
                                Detail = $"SubVertical '{subVertical}' is not valid for Vertical '{vertical}'.",
                                Status = StatusCodes.Status400BadRequest,
                                Instance = context.Request.Path
                            });
                        }
                    }

                    var result = await service.GetSearches(userId, vertical, subVertical);
                    return TypedResults.Ok(result);
                }
                catch (JsonException)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Token Error",
                        Detail = "Invalid user data format in token.",
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
.WithName("GetSavedSearches")
.WithTags("Search")
.WithSummary("Get saved searches")
.WithDescription("Get all saved searches for the current user filtered by vertical (mandatory) and optional subVertical.")
.RequireAuthorization()
.Produces<List<SavedSearchResponseDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/search/save-by-id", async Task<Results<
                Ok<List<SavedSearchResponseDto>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                [Required][FromQuery] string userId,
                [Required][FromQuery] Vertical vertical,
                [FromQuery] SubVertical? subVertical,
                IClassifiedService service,
                HttpContext context
            ) =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the query.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (subVertical.HasValue)
                {
                    var isValid = vertical switch
                    {
                        Vertical.Vehicles => subVertical.Value is SubVertical.Items or SubVertical.Deals or SubVertical.Preloved,
                        Vertical.Properties => subVertical.Value is SubVertical.Items or SubVertical.Deals,
                        Vertical.Rewards => subVertical.Value is SubVertical.Deals or SubVertical.Stores,
                        Vertical.Classifieds => subVertical.Value is SubVertical.Items or SubVertical.Preloved or SubVertical.Collectibles,
                        Vertical.Services => subVertical.Value is SubVertical.Services,
                        Vertical.Content => subVertical.Value is SubVertical.News or SubVertical.Daily or SubVertical.Events or SubVertical.Community,
                        _ => false
                    };

                    if (!isValid)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = $"SubVertical '{subVertical}' is not valid for Vertical '{vertical}'.",
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }
                }

                try
                {
                    var result = await service.GetSearches(userId, vertical, subVertical);
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
            })
            .WithName("GetSavedSearchById")
            .WithTags("Search")
            .WithSummary("Get saved searches by user ID")
            .WithDescription("Get all saved searches for a specific user filtered by vertical (mandatory) and optional subVertical.")
            .ExcludeFromDescription()
            .Produces<List<SavedSearchResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            // itemsAd post
            group.MapPost("items", async Task<IResult> (
                HttpContext httpContext,
                [FromBody] ClassifiedsItemsDTO dto,
                IClassifiedService service,
                [FromServices] IV2SubscriptionService subscriptionService,
                AuditLogger auditLogger,
                CancellationToken token) =>
            {
                string? uid = "unknown";
                string? subId = null;
                string? name = null;
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;
                    var slug = SlugHelper.GenerateSlug(dto.Title, dto.Category, "Classifieds", Guid.NewGuid());
                    var freeSub = (await subscriptionService.GetUserFreeSubscriptionsAsync(uid, token))
                                                             .OrderBy(s => s.EndDate)
                                                             .FirstOrDefault();
                    var subscriptionId = Guid.Empty;
                    var expiryDate = DateTime.MinValue;
                    if (freeSub is not null)
                    {
                        subscriptionId = freeSub.Id;
                        expiryDate = freeSub.EndDate;
                    }
                    var request = new Items
                    {
                        UserId = uid,
                        UserName = userName,
                        L2CategoryId = dto.L2CategoryId,
                        BuildingNumber = dto.BuildingNumber,
                        SubVertical = SubVertical.Items,
                        Slug = slug,
                        AdType = dto.AdType,
                        Title = dto.Title,
                        Description = dto.Description,
                        Price = dto.Price,
                        PriceType = dto.PriceType,
                        CategoryId = dto.CategoryId,
                        Category = dto.Category,
                        L1CategoryId = dto.L1CategoryId,
                        L1Category = dto.L1Category,
                        L2Category = dto.L2Category,
                        Brand = dto.Brand,
                        Model = dto.Model,
                        Color = dto.Color,
                        Condition = dto.Condition,
                        Location = dto.Location,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        ContactNumber = dto.ContactNumber,
                        ContactEmail = dto.ContactEmail,
                        WhatsAppNumber = dto.WhatsAppNumber,
                        StreetNumber = dto.StreetNumber,
                        zone = dto.zone,
                        ContactNumberCountryCode = dto.ContactNumberCountryCode,
                        WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                        ExpiryDate = expiryDate,
                        FeaturedExpiryDate = null,
                        IsFeatured = false,
                        IsPromoted = false,
                        LastRefreshedOn = null,
                        PromotedExpiryDate = null,
                        PublishedDate = null,
                        Status = AdStatus.Draft,
                        SubscriptionId = subscriptionId,
                        IsActive = true,
                        CreatedBy = userName,
                        CreatedAt = DateTime.UtcNow,
                        Images = dto.Images.Select(i => new ImageInfo
                        {
                            Url = i.Url,
                            Order = i.Order
                        }).ToList(),
                        Attributes = dto.Attributes

                    };

                    if (uid == null && name == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var response = await service.CreateClassifiedItemsAd(request, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "POST",
                        apiEndpoint: "/api/classifieds/items",
                        message: $"Classified items ad created successfully. Title: {dto.Title}",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                        );


                    return TypedResults.Created($"/api/classifieds/items/user-ads-by-id/{response.AdId}", response);

                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items", ex, uid, token);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items", ex, uid, token);
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
                Items dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == null)
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

            group.MapPut("/items/refresh", async Task<IResult> (
                HttpContext httpContext,
                [FromQuery] SubVertical subVertical,
                [FromQuery] long adId,
                IClassifiedService service,
                AuditLogger auditLogger,
                CancellationToken token) =>
            {
                string? uid = "unknown";
                string? subId = null;
                string? name = null;
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;

                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    Guid? subscriptionId = null;
                    DateTime? expiryDate = null;


                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(claim.Value))
                            {
                                var subscription = doc.RootElement;

                                if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                    verticalProp.GetInt32() == 3 &&
                                    subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                    subVerticalProp.GetInt32() == (int)subVertical)
                                {
                                    if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                    {

                                        expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                        subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (subscriptionId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No Items subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.RefreshClassifiedItemsAd(subVertical, adId, uid, subscriptionId.Value, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "PUT",
                        apiEndpoint: "/api/classifieds/items/refresh",
                        message: $"Ad with ID {adId} refreshed successfully.",
                        createdBy: uid,
                        payload: new { SubVertical = subVertical, AdId = adId },
                        cancellationToken: token
                        );

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/refresh", ex, uid, token);
                    return TypedResults.Problem(
                        title: "Error Refreshing Ad",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
  .RequireAuthorization()
  .WithName("RefreshItemsAd")
  .WithTags("Classified")
  .WithSummary("Refresh the ad (authorized)")
  .WithDescription("Refresh an ad by resetting CreatedDate and RefreshExpiryDate, requires login.")
  .Produces(StatusCodes.Status200OK)
  .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
  .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
  .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPut("/items/refreshed/{userId}/{adId}", async Task<IResult> (
                string userId,
                long adId,
                [FromQuery] SubVertical subVertical,
                Guid subscriptionId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (adId <= 0)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "AdId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.RefreshClassifiedItemsAd(subVertical, adId, userId, subscriptionId, token);

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Error Refreshing Ad",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .ExcludeFromDescription()
            .WithName("RefreshedItemsAd")
            .WithTags("Classified")
            .WithSummary("Refresh the ad (direct call)")
            .WithDescription("Refresh an ad by resetting CreatedDate and RefreshExpiryDate, using explicit userId.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            group.MapPost("preloved", async Task<IResult> (
     HttpContext httpContext,
     [FromBody] ClassifiedsPrelovedDTO dto,
     IClassifiedService service,
     AuditLogger auditLogger,
     CancellationToken token) =>
            {
                string? uid = "unknown";
                string? subId = null;
                string? name = null;
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;

                    // Get all subscription claims
                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    Guid? subscriptionId = null;
                    DateTime? expiryDate = null;

                    // Find Preloved subscription (Vertical=3 and SubVertical=4)
                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(claim.Value))
                            {
                                var subscription = doc.RootElement;

                                if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                    verticalProp.GetInt32() == 3 &&
                                    subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                    subVerticalProp.GetInt32() == 4)
                                {
                                    //subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                    //expiryDate = DateTime.Parse(subscription.GetProperty("EndDate").GetString());
                                    //break;
                                    if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                    {
                                        // Parse to DateTime and ensure UTC
                                        expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                        subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (subscriptionId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No Preloved subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var slug = SlugHelper.GenerateSlug(dto.Title, dto.Category, "Classifieds", Guid.NewGuid());
                    var request = new Preloveds
                    {
                        UserId = uid,
                        AuthenticityCertificateUrl = dto.AuthenticityCertificateUrl,
                        HasAuthenticityCertificate = dto.HasAuthenticityCertificate,
                        L2CategoryId = dto.L2CategoryId,
                        BuildingNumber = dto.BuildingNumber,
                        SubVertical = SubVertical.Preloved,
                        AdType = dto.AdType,
                        Title = dto.Title,
                        Slug = slug,
                        Description = dto.Description,
                        Price = dto.Price,
                        PriceType = dto.PriceType,
                        CategoryId = dto.CategoryId,
                        Category = dto.Category,
                        L1CategoryId = dto.L1CategoryId,
                        L1Category = dto.L1Category,
                        L2Category = dto.L2Category,
                        Brand = dto.Brand,
                        Model = dto.Model,
                        Color = dto.Color,
                        Condition = dto.Condition,
                        Location = dto.Location,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        ContactNumber = dto.ContactNumber,
                        ContactEmail = dto.ContactEmail,
                        WhatsAppNumber = dto.WhatsAppNumber,
                        StreetNumber = dto.StreetNumber,
                        ContactNumberCountryCode = dto.ContactNumberCountryCode,
                        WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                        ExpiryDate = expiryDate,
                        FeaturedExpiryDate = null,
                        IsFeatured = false,
                        IsPromoted = false,
                        LastRefreshedOn = null,
                        PromotedExpiryDate = null,
                        PublishedDate = null,
                        Status = AdStatus.Draft,
                        SubscriptionId = subscriptionId.Value,
                        Inclusion = dto.Inclusion,
                        UserName = userName,
                        zone = dto.zone,
                        IsActive = true,
                        CreatedBy = userName,
                        CreatedAt = DateTime.UtcNow,
                        Images = dto.Images.Select(i => new ImageInfo
                        {
                            Url = i.Url,
                            Order = i.Order
                        }).ToList(),
                        Attributes = dto.Attributes,
                    };

                    var result = await service.CreateClassifiedPrelovedAd(request, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "POST",
                        apiEndpoint: "/api/classifieds/preloved",
                        message: $"Classified preloved ad created successfully. Title: {dto.Title}",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                    );

                    return TypedResults.Created($"/api/classifieds/preloved/user-ads-by-id/{result.AdId}", result);
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/preloved", ex, uid, token);
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
                Preloveds dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == null)
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


            group.MapPut("items/update", async Task<IResult> (
                HttpContext httpContext,
                Items dto,
                IClassifiedService service,
                AuditLogger auditLogger,
                CancellationToken token) =>
            {
                string? uid = "unknown";
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return Results.Unauthorized();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();

                    if (uid == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var slug = SlugHelper.GenerateSlug(dto.Title, dto.Category, "Classifieds", Guid.NewGuid());
                    dto.UpdatedBy = uid;
                    dto.Slug = slug;

                    var result = await service.UpdateClassifiedItemsAd(dto, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "PUT",
                        apiEndpoint: "/api/classifieds/items/update",
                        message: $"Classified ad with ID {dto.Id} updated successfully.",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                        );

                    return TypedResults.Ok(result);
                }
                catch (ArgumentNullException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/update", ex, uid, token);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Missing Input",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/update", ex, uid, token);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/update", ex, uid, token);
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (OperationCanceledException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/update", ex, uid, token);
                    return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
                }
                catch (InvalidOperationException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/update", ex, uid, token);
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/update", ex, uid, token);
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Unhandled Error",
                        Detail = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("UpdateItemsClassifiedAd")
                .WithTags("Classified")
                .WithSummary("Update items classified ad using authenticated user")
                .WithDescription("Updates the ad details")
                .Produces<AdUpdatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPut("items/update-by-id", async Task<IResult> (
                Items dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UpdatedBy == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.UpdateClassifiedItemsAd(dto, token);

                    return TypedResults.Ok(result);
                }
                catch (ArgumentNullException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Missing Input",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Unhandled Error",
                        Detail = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("UpdateItemsClassifiedAdById")
                .WithTags("Classified")
                .WithSummary("Update items classified ad using provided UserId")
                .WithDescription("For admin or service scenarios where the UserId is passed explicitly.")
                .Produces<AdUpdatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPut("preloved/update", async Task<IResult> (
                HttpContext httpContext,
                Preloveds dto,
                IClassifiedService service,
                AuditLogger auditLogger,
                CancellationToken token) =>
            {
                string? uid = "unknown";
                string? subId = null;
                string? name = null;
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;

                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    Guid? subscriptionId = null;
                    DateTime? expiryDate = null;

                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(claim.Value))
                            {
                                var subscription = doc.RootElement;

                                if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                    verticalProp.GetInt32() == 3 &&
                                    subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                    subVerticalProp.GetInt32() == 4)
                                {
                                    if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                    {
                                        expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                        subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (subscriptionId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No Preloved subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var slug = SlugHelper.GenerateSlug(dto.Title, dto.Category, "Classifieds", Guid.NewGuid());
                    dto.Slug = slug;
                    dto.UpdatedBy = uid;

                    var result = await service.UpdateClassifiedPrelovedAd(dto, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "PUT",
                        apiEndpoint: "/api/classifieds/preloved/update",
                        message: "Preloved ad updated successfully",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                        );

                    return TypedResults.Ok(result);
                }
                catch (ArgumentNullException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/preloved/update", ex, uid, token);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Missing Input",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/preloved/update", ex, uid, token);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/preloved/update", ex, uid, token);
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
                }
                catch (InvalidOperationException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/preloved/update", ex, uid, token);
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/preloved/update", ex, uid, token);
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Unhandled Error",
                        Detail = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("UpdatePrelovedClassifiedAd")
                .WithTags("Classified")
                .WithSummary("Update preloved classified ad using authenticated user")
                .WithDescription("Updates the ad details")
                .Produces<AdUpdatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPut("preloved/update-by-id", async Task<IResult> (
                Preloveds dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UpdatedBy == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.UpdateClassifiedPrelovedAd(dto, token);

                    return TypedResults.Ok(result);
                }
                catch (ArgumentNullException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Missing Input",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Unhandled Error",
                        Detail = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("UpdatePrelovedClassifiedAdById")
                .WithTags("Classified")
                .WithSummary("Update preloved classified ad using provided UserId")
                .WithDescription("For admin or service scenarios where the UserId is passed explicitly.")
                .Produces<AdUpdatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPut("collectibles/update", async Task<IResult> (
               HttpContext httpContext,
               Collectibles dto,
               IClassifiedService service,
               AuditLogger auditLogger,
               CancellationToken token) =>
            {
                string uid = "unknown";
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;

                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    Guid? subscriptionId = null;
                    DateTime? expiryDate = null;

                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(claim.Value))
                            {
                                var subscription = doc.RootElement;

                                if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                    verticalProp.GetInt32() == 3 &&
                                    subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                    subVerticalProp.GetInt32() == 1)
                                {
                                    if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                    {
                                        expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                        subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (subscriptionId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No Collectibles subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var slug = SlugHelper.GenerateSlug(dto.Title, dto.Category, "Classifieds", Guid.NewGuid());
                    dto.Slug = slug;
                    dto.UpdatedBy = uid;

                    var result = await service.UpdateClassifiedCollectiblesAd(dto, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "PUT",
                        apiEndpoint: "/api/classifieds/collectibles/update",
                        message: "collectibles ad updated successfully",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                        );

                    return TypedResults.Ok(result);
                }
                catch (ArgumentNullException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/collectibles/update", ex, uid, token);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Missing Input",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/collectibles/update", ex, uid, token);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/collectibles/update", ex, uid, token);
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
                }
                catch (InvalidOperationException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/collectibles/update", ex, uid, token);
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/collectibles/update", ex, uid, token);
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Unhandled Error",
                        Detail = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
               .WithName("UpdateCollectiblesClassifiedAd")
               .WithTags("Classified")
               .WithSummary("Update collectibles classified ad using authenticated user")
               .WithDescription("Updates the ad details")
               .Produces<AdUpdatedResponseDto>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
               .RequireAuthorization();

            group.MapPut("collectibles/update-by-id", async Task<IResult> (
                Collectibles dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UpdatedBy == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.UpdateClassifiedCollectiblesAd(dto, token);

                    return TypedResults.Ok(result);
                }
                catch (ArgumentNullException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Missing Input",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Unhandled Error",
                        Detail = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("UpdateCollectiblesClassifiedAdById")
                .WithTags("Classified")
                .WithSummary("Update collectibles classified ad using provided UserId")
                .WithDescription("For admin or service scenarios where the UserId is passed explicitly.")
                .Produces<AdUpdatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPut("deals/update", async Task<IResult> (
             HttpContext httpContext,
             Deals dto,
             IClassifiedService service,
             AuditLogger auditLogger,
             CancellationToken token) =>
            {
                string uid = "unknown";
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;

                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    Guid? subscriptionId = null;
                    DateTime? expiryDate = null;

                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(claim.Value))
                            {
                                var subscription = doc.RootElement;

                                if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                    verticalProp.GetInt32() == 3 &&
                                    subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                    subVerticalProp.GetInt32() == 2)
                                {
                                    if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                    {
                                        expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                        subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (subscriptionId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No Deals subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var slug = SlugHelper.GenerateSlug(dto.Offertitle, dto.BusinessName, "Classifieds", Guid.NewGuid());
                    dto.Slug = slug;
                    dto.CreatedBy = userName;
                    dto.UpdatedBy = uid;

                    var result = await service.UpdateClassifiedDealsAd(dto, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "PUT",
                        apiEndpoint: "/api/classifieds/deals/update",
                        message: "Deals ad updated successfully",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                        );

                    return TypedResults.Ok(result);
                }
                catch (ArgumentNullException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/deals/update", ex, uid, token);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Missing Input",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/deals/update", ex, uid, token);
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/deals/update", ex, uid, token);
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
                }
                catch (InvalidOperationException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/deals/update", ex, uid, token);
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/deals/update", ex, uid, token);
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Unhandled Error",
                        Detail = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
             .WithName("UpdateDealsClassifiedAd")
             .WithTags("Classified")
             .WithSummary("Update Deals classified ad using authenticated user")
             .WithDescription("Updates the ad details")
             .Produces<AdUpdatedResponseDto>(StatusCodes.Status200OK)
             .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
             .RequireAuthorization();

            group.MapPut("deals/update-by-id", async Task<IResult> (
                Deals dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UpdatedBy == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.UpdateClassifiedDealsAd(dto, token);

                    return TypedResults.Ok(result);
                }
                catch (ArgumentNullException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Missing Input",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (OperationCanceledException)
                {
                    return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(new ProblemDetails
                    {
                        Title = "Unhandled Error",
                        Detail = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("UpdateDealsClassifiedAdById")
                .WithTags("Classified")
                .WithSummary("Update Deals classified ad using provided UserId")
                .WithDescription("For admin or service scenarios where the UserId is passed explicitly.")
                .Produces<AdUpdatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("collectibles", async Task<IResult> (
                HttpContext httpContext,
                [FromBody] ClassifiedsCollectablesDTO dto,
                IClassifiedService service,
                AuditLogger auditLogger,
                CancellationToken token) =>
            {
                string? uid = "unknown";
                string? subId = null;
                string? name = null;
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;

                    // Get all subscription claims
                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    Guid? subscriptionId = null;
                    DateTime? expiryDate = null;

                    // Find collectible subscription (Vertical=3 and SubVertical=4)
                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(claim.Value))
                            {
                                var subscription = doc.RootElement;

                                if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                    verticalProp.GetInt32() == 3 &&
                                    subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                    subVerticalProp.GetInt32() == 4)
                                {

                                    if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                    {
                                        // Parse to DateTime and ensure UTC
                                        expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                        subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (subscriptionId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No Collectibles subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var slug = SlugHelper.GenerateSlug(dto.Title, dto.Category, "Classifieds", Guid.NewGuid());
                    var request = new Collectibles
                    {
                        UserId = uid,
                        UserName = userName,
                        L2CategoryId = dto.L2CategoryId,
                        BuildingNumber = dto.BuildingNumber,
                        AuthenticityCertificateName = dto.AuthenticityCertificateName,
                        AuthenticityCertificateUrl = dto.AuthenticityCertificateUrl,
                        HasAuthenticityCertificate = dto.HasAuthenticityCertificate,
                        SubVertical = SubVertical.Collectibles,
                        AdType = dto.AdType,
                        Title = dto.Title,
                        Slug = slug,
                        Description = dto.Description,
                        Price = dto.Price,
                        PriceType = dto.PriceType,
                        CategoryId = dto.CategoryId,
                        Category = dto.Category,
                        L1CategoryId = dto.L1CategoryId,
                        L1Category = dto.L1Category,
                        L2Category = dto.L2Category,
                        Brand = dto.Brand,
                        Model = dto.Model,
                        Color = dto.Color,
                        Condition = dto.Condition,
                        Location = dto.Location,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        ContactNumber = dto.ContactNumber,
                        ContactEmail = dto.ContactEmail,
                        WhatsAppNumber = dto.WhatsAppNumber,
                        StreetNumber = dto.StreetNumber,
                        ContactNumberCountryCode = dto.ContactNumberCountryCode,
                        WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                        ExpiryDate = expiryDate,
                        FeaturedExpiryDate = null,
                        IsFeatured = false,
                        IsPromoted = false,
                        PromotedExpiryDate = null,
                        PublishedDate = null,
                        Status = AdStatus.Draft,
                        SubscriptionId = subscriptionId,
                        HasWarranty = dto.HasWarranty,
                        IsHandmade = dto.IsHandmade,
                        YearOrEra = dto.YearOrEra,
                        zone = dto.zone,
                        IsActive = true,
                        CreatedBy = userName,
                        CreatedAt = DateTime.UtcNow,
                        Images = dto.Images.Select(i => new ImageInfo
                        {
                            Url = i.Url,
                            Order = i.Order
                        }).ToList(),
                        Attributes = dto.Attributes,

                    };

                    var result = await service.CreateClassifiedCollectiblesAd(request, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "POST",
                        apiEndpoint: "/api/classifieds/collectibles",
                        message: "Collectibles ad created successfully",
                        createdBy: uid,
                        payload: request,
                        cancellationToken: token
                        );

                    return TypedResults.Created(
                        $"/api/classifieds/collectibles/user-ads-by-id/{result.AdId}", result);
                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/collectibles", ex, uid, token);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/collectibles", ex, uid, token);
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
                Collectibles dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == null)
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



            group.MapPost("deals", async Task<IResult> (
                HttpContext httpContext,
                [FromBody] ClassifiedsDealsDTO dto,
                IClassifiedService service,
                AuditLogger auditLogger,
                CancellationToken token) =>
            {
                string? uid = "unknown";
                string? subId = null;
                string? name = null;
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;

                    // Get all subscription claims
                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    Guid? subscriptionId = null;
                    DateTime? expiryDate = null;

                    // Find collectible subscription (Vertical=3 and SubVertical=4)
                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(claim.Value))
                            {
                                var subscription = doc.RootElement;

                                if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                    verticalProp.GetInt32() == 3 &&
                                    subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                    subVerticalProp.GetInt32() == 2)
                                {

                                    if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                    {
                                        // Parse to DateTime and ensure UTC
                                        expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                        subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (subscriptionId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No Deals subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var slug = SlugHelper.GenerateSlug(dto.Offertitle, dto.StartDate.ToString(), "Classifieds", Guid.NewGuid());

                    var request = new Deals
                    {
                        UserId = uid,
                        Description = dto.Description,
                        Slug = slug,
                        IsActive = true,
                        CreatedBy = userName,
                        CreatedAt = DateTime.UtcNow,
                        FlyerFileUrl = dto.FlyerFileUrl,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        DataFeedUrl = dto.DataFeedUrl,
                        WebsiteUrl = dto.WebsiteUrl,
                        ContactNumber = dto.ContactNumber,
                        WhatsappNumber = dto.WhatsAppNumber,
                        CoverImage = dto.CoverImage,
                        Locations = dto.Locations,
                        ExpiryDate = dto.ExpiryDate,
                        FeaturedExpiryDate = null,
                        IsFeatured = false,
                        IsPromoted = false,
                        PromotedExpiryDate = null,
                        SubscriptionId = subscriptionId,
                        XMLlink = dto.XMLlink,
                        Offertitle = dto.Offertitle,
                    };


                    //dto.UserId = uid;
                    var result = await service.CreateClassifiedDealsAd(request, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "POST",
                        apiEndpoint: "/api/classifieds/deals",
                        message: "Deals ad created successfully",
                        createdBy: uid,
                        payload: request,
                        cancellationToken: token
                        );

                    return TypedResults.Created($"/api/classifieds/deals/user-ads-by-id/{result.AdId}", result);

                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/deals", ex, uid, token);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/deals", ex, uid, token);
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/deals", ex, uid, token);
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
                Deals dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == null)
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


            group.MapDelete("/classified/{subVertical}/{adId:long}", async Task<Results<
                Ok<DeleteAdResponseDto>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                SubVertical subVertical,
                long adId,
                IClassifiedService service,
                AuditLogger auditLogger,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                string uid = context.User.FindFirst("sub")?.Value;
                string userName = context.User.FindFirst("preferred_username")?.Value;

                if (adId <= 0)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid positive number.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var response = await service.DeleteClassifiedAd(subVertical, adId, uid, cancellationToken);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "DELETE",
                        apiEndpoint: $"/api/classified/{subVertical}/{adId}",
                        message: $"Deleted classified ad with ID {adId} in {subVertical}",
                        createdBy: uid,
                        payload: new { SubVertical = subVertical.ToString(), AdId = adId },
                        cancellationToken: cancellationToken
                        );

                    return TypedResults.Ok(response);
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", $"/api/classified/{subVertical}/{adId}", ex, uid, cancellationToken);
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
                    await auditLogger.LogExceptionAsync("Classified", $"/api/classified/{subVertical}/{adId}", ex, uid, cancellationToken);
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
                    await auditLogger.LogExceptionAsync("Classified", $"/api/classified/{subVertical}/{adId}", ex, uid, cancellationToken);
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested classified ad not found.",
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
                .WithName("DeleteClassifiedAd")
                .WithTags("Classified")
                .WithSummary("Delete a classified ad by ID and subVertical")
                .WithDescription("Deletes a classified ad using the provided subVertical and Ad ID. User must own the ad.")
                .Produces<DeleteAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapDelete("/{subVertical}/delete-by-id/{adId:long}/{userId}", async Task<Results<
                Ok<DeleteAdResponseDto>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                SubVertical subVertical,
                long adId,
                string userId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "UserId must not be empty.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (adId <= 0)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid positive number.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var response = await service.DeleteClassifiedAd(subVertical, adId, userId, cancellationToken);
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
                            Detail = "Requested classified ad not found.",
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
                .WithName("DeleteClassifiedAdById")
                .WithTags("Classified")
                .WithSummary("Delete a classified ad by ID, UserId, and subVertical")
                .WithDescription("Deletes a classified ad using the provided subVertical, Ad ID, and UserId. Used by admin/service scenarios.")
                .Produces<DeleteAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("/items/{adId}", async Task<IResult> (
                long adId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (adId <= 0)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid GUID.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetItemAdById(adId, token);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Ad Not Found",
                            Detail = $"No published item ad was found with ID '{adId}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
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
                .WithName("GetItemAdById")
                .WithTags("Classified")
                .WithSummary("Get a Active item ad by ID")
                .WithDescription("Retrieves the full Active item ad details based on the provided Ad ID.")
                .Produces<Items>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/items/slug/{slug}", async Task<IResult> (
                string slug,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(slug))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Slug must not be null or empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetItemAdBySlug(slug, token);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Ad Not Found",
                            Detail = $"No active item ad was found with Slug '{slug}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
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
                .WithName("GetItemAdBySlug")
                .WithTags("Classified")
                .WithSummary("Get an Active item ad by Slug")
                .WithDescription("Retrieves the full Active item ad details based on the provided Slug.")
                .Produces<Items>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/preloved/slug/{slug}", async Task<IResult> (
                string slug,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(slug))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Slug must not be null or empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetPrelovedAdBySlug(slug, token);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Ad Not Found",
                            Detail = $"No active collectibles ad was found with Slug '{slug}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
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
                .WithName("GetPrelovedAdBySlug")
                .WithTags("Classified")
                .WithSummary("Get an Active preloved ad by Slug")
                .WithDescription("Retrieves the full Active preloved ad details based on the provided Slug.")
                .Produces<Preloveds>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/collectibles/slug/{slug}", async Task<IResult> (
                string slug,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(slug))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Slug must not be null or empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetCollectiblesAdBySlug(slug, token);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Ad Not Found",
                            Detail = $"No active collectibles ad was found with Slug '{slug}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
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
                .WithName("GetCollectiblesAdBySlug")
                .WithTags("Classified")
                .WithSummary("Get an Active preloved ad by Slug")
                .WithDescription("Retrieves the full Active preloved ad details based on the provided Slug.")
                .Produces<Collectibles>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/deals/slug/{slug}", async Task<IResult> (
                string slug,
                IClassifiedService service,
                CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(slug))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Slug must not be null or empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetDealsAdBySlug(slug, token);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Ad Not Found",
                            Detail = $"No active deals ad was found with Slug '{slug}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
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
                .WithName("GetdealsAdBySlug")
                .WithTags("Classified")
                .WithSummary("Get an Active deals ad by Slug")
                .WithDescription("Retrieves the full Active deals ad details based on the provided Slug.")
                .Produces<Deals>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/items/my-ads", async Task<Results<
                Ok<List<Items>>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    string uid = context.User.FindFirst("sub")?.Value;
                    string userName = context.User.FindFirst("preferred_username")?.Value;

                    var ads = await service.GetAllItemsAdByUser(uid, cancellationToken);

                    return TypedResults.Ok(ads);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("GetMyItemsAds")
                .WithTags("Classified")
                .WithSummary("Get my Items ads")
                .WithDescription("Returns the authenticated user's active Items ads.")
                .Produces<List<Items>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("/items/by-user/{userId}", async Task<Results<
                Ok<List<Items>>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                string userId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "UserId must not be empty.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var ads = await service.GetAllItemsAdByUser(userId, cancellationToken);
                    return TypedResults.Ok(ads);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("GetItemsAdsByUserId")
                .WithTags("Classified")
                .WithSummary("Get Items ads by UserId")
                .WithDescription("Returns active Items ads for the specified userId. Intended for admin/service scenarios.")
                .Produces<List<Items>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("/preloved/my-ads", async Task<Results<
                Ok<List<Preloveds>>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    string uid = context.User.FindFirst("sub")?.Value;
                    string userName = context.User.FindFirst("preferred_username")?.Value;

                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must not be empty.",
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }

                    var ads = await service.GetAllPrelovedAdByUser(uid, cancellationToken);

                    return TypedResults.Ok(ads);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("GetMyPrelovedAds")
                .WithTags("Classified")
                .WithSummary("Get my Preloved ads")
                .WithDescription("Returns the authenticated user's active Preloved ads.")
                .Produces<List<Preloveds>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("/preloved/by-user/{userId}", async Task<Results<
                Ok<List<Preloveds>>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                string userId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "UserId must not be empty.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var ads = await service.GetAllPrelovedAdByUser(userId, cancellationToken);
                    return TypedResults.Ok(ads);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("GetPrelovedAdsByUserId")
                .WithTags("Classified")
                .WithSummary("Get Preloved ads by UserId")
                .WithDescription("Returns active Preloved ads for the specified userId. Intended for admin/service scenarios.")
                .Produces<List<Preloveds>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("/collectibles/my-ads", async Task<Results<
                Ok<List<Collectibles>>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    string uid = context.User.FindFirst("sub")?.Value;
                    string userName = context.User.FindFirst("preferred_username")?.Value;

                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must not be empty.",
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }

                    var ads = await service.GetAllCollectiblesAdByUser(uid, cancellationToken);

                    return TypedResults.Ok(ads);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("GetMyCollectiblesAds")
                .WithTags("Classified")
                .WithSummary("Get my Collectibles ads")
                .WithDescription("Returns the authenticated user's active Collectibles ads.")
                .Produces<List<Collectibles>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("/collectibles/by-user/{userId}", async Task<Results<
                Ok<List<Collectibles>>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                string userId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "UserId must not be empty.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var ads = await service.GetAllCollectiblesAdByUser(userId, cancellationToken);
                    return TypedResults.Ok(ads);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("GetCollectiblesByUserId")
                .WithTags("Classified")
                .WithSummary("Get Collectibles ads by UserId")
                .WithDescription("Returns active Collectibles ads for the specified userId. Intended for admin/service scenarios.")
                .Produces<List<Collectibles>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("/deals/my-ads", async Task<Results<
                Ok<List<Deals>>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    string uid = context.User.FindFirst("sub")?.Value ?? "unknown";
                    string userName = context.User.FindFirst("preferred_username")?.Value ?? "unknown";

                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must not be empty.",
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.Request.Path
                        });
                    }

                    var ads = await service.GetAllDealsAdByUser(uid, cancellationToken);

                    return TypedResults.Ok(ads);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("GetMyDealsAds")
                .WithTags("Classified")
                .WithSummary("Get my Deals ads")
                .WithDescription("Returns the authenticated user's active Deals ads.")
                .Produces<List<Deals>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapGet("/deals/by-user/{userId}", async Task<Results<
                Ok<List<Deals>>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                string userId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "UserId must not be empty.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var ads = await service.GetAllDealsAdByUser(userId, cancellationToken);
                    return TypedResults.Ok(ads);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("GetDealsByUserId")
                .WithTags("Classified")
                .WithSummary("Get Deals ads by UserId")
                .WithDescription("Returns active Deals ads for the specified userId. Intended for admin/service scenarios.")
                .Produces<List<Deals>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();


            group.MapGet("/preloved/{adId}", async Task<IResult> (
                long adId,
                IClassifiedService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetPrelovedAdById(adId, cancellationToken);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Ad Not Found",
                            Detail = $"No Preloved ad found with ID: {adId}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
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
                .WithName("GetPrelovedAdById")
                .WithTags("Classified")
                .WithSummary("Get a single Preloved Ad by ID")
                .WithDescription("Fetches the details of a specific Preloved ad using the provided adId.")
                .Produces<ClassifiedsPreloved>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/deals/{adId}", async Task<IResult> (
                long adId,
                IClassifiedService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetDealsAdById(adId, cancellationToken);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Ad Not Found",
                            Detail = $"No Deals ad found with ID: {adId}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
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
                .WithName("GetDealsAdById")
                .WithTags("Classified")
                .WithSummary("Get a single Deals Ad by ID")
                .WithDescription("Fetches the details of a specific Deals ad using the provided adId.")
                .Produces<ClassifiedsDeals>(StatusCodes.Status200OK)
                .Produces<DealsAdDto>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/collectibles/{adId}", async Task<IResult> (
                long adId,
                IClassifiedService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetCollectiblesAdById(adId, cancellationToken);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Ad Not Found",
                            Detail = $"No Collectibles ad found with ID: {adId}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.InnerException?.Message ?? ex.Message,
                        Status = StatusCodes.Status404NotFound
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
                .WithName("GetCollectiblesAdById")
                .WithTags("Classified")
                .WithSummary("Get a single Collectibles Ad by ID")
                .WithDescription("Fetches the details of a specific Collectibles ad using the provided adId.")
                .Produces<ClassifiedsCollectibles>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



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
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

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

            group.MapGet("/user-dashboard", async Task<IResult> (
                HttpContext context,
                [FromQuery] SubVertical subVertical,
                [FromQuery] bool? isPublished,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] string? search,
                IClassifiedService service,
                CancellationToken token) =>
                    {
                        var uid = context.User.FindFirst("sub")?.Value ?? "unknown";
                        var userName = context.User.FindFirst("preferred_username")?.Value ?? "unknown";

                        if (uid == null)
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
                            var result = await service.GetFilteredAds(subVertical, isPublished, page ?? 1, pageSize ?? 10, search, uid, token);
                            return TypedResults.Ok(result);
                        }
                        catch (ArgumentException argEx)
                        {
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Invalid request",
                                Detail = argEx.Message,
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
            .WithName("GetFilteredClassifiedAdsForUser")
            .WithTags("Classified")
            .WithSummary("Get filtered classified ads for the authenticated user")
            .WithDescription("Supports filters like subVertical, isPublished, search, and pagination for current user.")
            .Produces<PaginatedAdResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();


            group.MapGet("/user-dashborad/{userId}", async Task<IResult> (
              [FromQuery] string userId,
              [FromQuery] SubVertical subVertical,
              [FromQuery] bool? isPublished,
              [FromQuery] int? page,
              [FromQuery] int? pageSize,
              [FromQuery] string? search,
              IClassifiedService service,
              CancellationToken token) =>
            {

                if (userId == null)
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
                    var result = await service.GetFilteredAds(subVertical, isPublished, page ?? 1, pageSize ?? 10, search, userId, token);
                    return TypedResults.Ok(result);
                }
                catch (ArgumentException argEx)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid request",
                        Detail = argEx.Message,
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
           .WithName("GetFilteredClassifiedAdsByUserId")
           .WithTags("Classified")
           .WithSummary("Get filtered classified ads by specific user ID")
           .WithDescription("Admin or service endpoint to retrieve filtered ads using subVertical, isPublished, and userId.")
           .Produces<PaginatedAdResponseDto>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .ExcludeFromDescription()
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            group.MapPut("/items/promote", async Task<IResult> (
    HttpContext httpContext,
    ClassifiedsPromoteDto dto,
    IClassifiedService service,
    AuditLogger auditLogger,
    CancellationToken token) =>
            {
                string? uid = "unknown";
                string? subId = null;
                string? name = null;
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;

                    // Get all subscription claims
                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    Guid? subscriptionId = null;
                    DateTime? expiryDate = null;

                    // Find Preloved subscription (Vertical=3 and SubVertical=4)
                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(claim.Value))
                            {
                                var subscription = doc.RootElement;

                                if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                    verticalProp.GetInt32() == 3 &&
                                    subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                    subVerticalProp.GetInt32() == (int)dto.SubVertical)
                                {
                                    if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                    {

                                        expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                        subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (subscriptionId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No items subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    await service.PromoteClassifiedAd(dto, uid, subscriptionId.Value, token);

                    // Audit log
                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "PUT",
                        apiEndpoint: "/api/classifieds/items/promote",
                        message: $"Promoted classified ad with ID {dto.AdId}",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                    );

                    return TypedResults.Ok(new
                    {
                        AdId = dto.AdId,
                        Message = "The ad has been successfully marked as promoted."
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/promote", ex, uid, token);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
.RequireAuthorization()
.WithName("PromoteItemsAd")
.WithTags("Classified")
.WithSummary("Promote the ad's 'IsPromoted' field, set the 'CreatedDate' to current date")
.WithDescription("Updates the ad's 'IsPromoted' field to true, the 'CreatedDate' to the current date")
.Produces(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPut("/promoted/{userId}/{adId}",
      async Task<IResult> (
          string userId,
          ClassifiedsPromoteDto dto,
          Guid subscriptionid,
          IClassifiedService service,
          CancellationToken token) =>
      {
          try
          {
              if (dto.AdId <= 0)
              {
                  return TypedResults.BadRequest(new ProblemDetails
                  {
                      Title = "Validation Error",
                      Detail = "AdId is required.",
                      Status = StatusCodes.Status400BadRequest
                  });
              }

              if (string.IsNullOrWhiteSpace(userId))
              {
                  return TypedResults.BadRequest(new ProblemDetails
                  {
                      Title = "Validation Error",
                      Detail = "UserId is required.",
                      Status = StatusCodes.Status400BadRequest
                  });
              }

              var Dto = new ClassifiedsPromoteDto
              {
                  AdId = dto.AdId,
                  SubVertical = dto.SubVertical,
                  IsPromoted = dto.IsPromoted

              };

              await service.PromoteClassifiedAd(dto, userId, subscriptionid, token);

              return TypedResults.Ok(new
              {
                  AdId = dto.AdId,
                  Message = dto.IsPromoted == true
        ? "The ad has been successfully marked as promoted."
        : "The ad has been successfully marked as unpromoted."
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
                .ExcludeFromDescription()
.WithName("PromotedItemsAd")
.WithTags("Classified")
.WithSummary("Promote the ad's 'IsPromoted' field, set the 'CreatedDate' to current date")
.WithDescription("Updates the ad's 'IsPromoted' field to true, the 'CreatedDate' to the current date")
.Produces(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPut("/items/feature", async Task<IResult> (
              HttpContext httpContext,
             ClassifiedsPromoteDto dto,
             AuditLogger auditLogger,
             IClassifiedService service,
             CancellationToken token) =>
            {
                string? uid = "unknown";
                string? subId = null;
                string? name = null;
                try
                {
                    uid = httpContext.User.FindFirst("sub")?.Value;
                    var userName = httpContext.User.FindFirst("preferred_username")?.Value;

                    // Get all subscription claims
                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    Guid? subscriptionId = null;
                    DateTime? expiryDate = null;

                    // Find Preloved subscription (Vertical=3 and SubVertical=4)
                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(claim.Value))
                            {
                                var subscription = doc.RootElement;

                                if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                    verticalProp.GetInt32() == 3 &&
                                    subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                    subVerticalProp.GetInt32() == (int)dto.SubVertical)
                                {
                                    if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                    {

                                        expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                        subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (subscriptionId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No Items subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    await service.FeatureClassifiedAd(dto, uid, subscriptionId.Value, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "PUT",
                        apiEndpoint: "/api/classifieds/items/feature",
                        message: $"Featured classified ad with ID {dto.AdId}",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                        );

                    return TypedResults.Ok(new
                    {
                        AdId = dto.AdId,
                        Message = "The ad has been successfully marked as featured."
                    });
                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/feature", ex, uid, token);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/feature", ex, uid, token);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/feature", ex, uid, token);
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/feature", ex, uid, token);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .RequireAuthorization()
                .WithName("FeatureItemssAd")
                .WithTags("Classified")
                .WithSummary("Feature the ad's 'IsFeatured' field, set the 'CreatedDate' to current date")
                .WithDescription("Updates the ad's 'IsFeatured' field to true, the 'CreatedDate' to the current date")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/featured", async Task<IResult> (
                ClassifiedsPromoteDto dto,
                string userId,
                Guid subscriptionid,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.AdId <= 0)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "AdId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    await service.FeatureClassifiedAd(dto, userId, subscriptionid, token);
                    return TypedResults.Ok(new
                    {
                        AdId = dto.AdId,
                        Message = "The ad has been successfully marked as featured."
                    });
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
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
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
                .ExcludeFromDescription()
                .WithName("FeaturedItemssAd")
                .WithTags("Classified")
                .WithSummary("Feature the ad's 'IsFeatured' field, set the 'CreatedDate' to current date")
                .WithDescription("Updates the ad's 'IsFeatured' field to true, the 'CreatedDate' to the current date")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("user-dashboard/bulk-action", async Task<IResult> (
                HttpContext context,
                [FromQuery] int subVertical,
                [FromQuery] bool isPublished,
                [FromBody] List<long> adIds,
                IClassifiedService service,
                AuditLogger auditLogger,
                CancellationToken token) =>
                    {
                        string? uid = "unknown";
                        string? subId = null;
                        string? name = null;
                        try
                        {
                            uid = context.User.FindFirst("sub")?.Value;
                            var userName = context.User.FindFirst("preferred_username")?.Value;

                            
                            var subscriptionClaims = context.User.FindAll("subscriptions").ToList();
                            Guid? subscriptionId = null;
                            DateTime? expiryDate = null;

                            // Find collectible subscription (Vertical=3 and SubVertical=4)
                            foreach (var claim in subscriptionClaims)
                            {
                                try
                                {
                                    using (var doc = JsonDocument.Parse(claim.Value))
                                    {
                                        var subscription = doc.RootElement;

                                        if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                            verticalProp.GetInt32() == 3 &&
                                            subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                            subVerticalProp.GetInt32() == 4)
                                        {

                                            if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                            endDateProp.ValueKind == JsonValueKind.String)
                                            {
                                                // Parse to DateTime and ensure UTC
                                                expiryDate = DateTime.Parse(endDateProp.GetString()).ToUniversalTime();
                                                subscriptionId = Guid.Parse(subscription.GetProperty("Id").GetString());
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }

                            if (subscriptionId == null)
                            {
                                return TypedResults.BadRequest(new ProblemDetails
                                {
                                    Title = "Subscription Required",
                                    Detail = "No subscription found for this user.",
                                    Status = StatusCodes.Status400BadRequest
                                });
                            }

                            var result = await service.BulkUpdateAdPublishStatusAsync(
                                subVertical,
                                uid,
                                adIds,
                                isPublished, subscriptionId.Value, token);

                            await auditLogger.LogAuditAsync(
                                module: "Classified",
                                httpMethod: "POST",
                                apiEndpoint: "/api/classifieds/user-dashboard/bulk-action",
                                message: $"{(isPublished ? "Published" : "Unpublished")} {adIds.Count} classified ads",
                                createdBy: uid,
                                payload: new { subVertical, adIds, isPublished },
                                cancellationToken: token
                                );

                            return TypedResults.Ok(result);
                        }
                        catch (InvalidOperationException ex)
                        {
                            await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/user-dashboard/bulk-action", ex, uid, token);
                            return TypedResults.Conflict(new ProblemDetails
                            {
                                Title = isPublished ? "Publish Failed" : "Unpublish Failed",
                                Detail = ex.Message,
                                Status = StatusCodes.Status409Conflict
                            });
                        }
                        catch (Exception ex)
                        {
                            await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/user-dashboard/bulk-action", ex, uid, token);
                            return TypedResults.Problem(
                                title: "Internal Server Error",
                                detail: ex.Message,
                                statusCode: StatusCodes.Status500InternalServerError);
                        }
                    })
            .WithName("BulkPublishDynamic")
            .WithTags("Classified")
            .WithSummary("Bulk publish/unpublish ads (dynamic by sub-vertical)")
            .WithDescription("Publishes or unpublishes ads across sub-verticals for the authenticated user. Parameters in query, adIds in body.")
            .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            group.MapPost("user-dashboard/bulk-action-by-id", async Task<IResult> (
                [FromQuery] int subVertical,
                [FromQuery] bool isPublished,
                [FromQuery] string userId,
                [FromBody] List<long> adIds,
                 Guid subscriptionid,
                IClassifiedService service,
                CancellationToken token) =>
                    {
                        if (string.IsNullOrWhiteSpace(userId))
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
                            var result = await service.BulkUpdateAdPublishStatusAsync(
                                subVertical,
                                userId,
                                adIds,
                                isPublished, subscriptionid,
                                token);

                            return TypedResults.Ok(result);
                        }
                        catch (InvalidOperationException ex)
                        {
                            return TypedResults.Conflict(new ProblemDetails
                            {
                                Title = isPublished ? "Publish Failed" : "Unpublish Failed",
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
                    }).WithName("BulkPublishDynamicbyid")
        .WithTags("Classified")
        .WithSummary("Admin: Bulk publish/unpublish ads for any user")
        .WithDescription("Used by admins to bulk update ad publish status for any user across sub-verticals.")
        .Produces<BulkAdActionResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .ExcludeFromDescription();


            #region Wishlist
            group.MapPost("wishlist/favourite", async Task<IResult> (
                HttpContext httpContext,
                WishlistCreateDto dto,
                IClassifiedService service,
                AuditLogger auditLogger,
                CancellationToken token) =>
            {
                string? uid = null;
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return Results.Unauthorized();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();

                    if (string.IsNullOrEmpty(uid))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    await service.Favourite(dto, uid, token);

                    await auditLogger.LogAuditAsync(
                        module: "Wishlist",
                        httpMethod: "POST",
                        apiEndpoint: "/api/wishlist/favourite",
                        message: $"Item favourited successfully. AdId: {dto.AdId}",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                    );

                    return TypedResults.Ok(new { Message = "Added to favourites successfully." });
                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Wishlist", "/api/wishlist/favourite", ex, uid, token);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Wishlist", "/api/wishlist/favourite", ex, uid, token);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("FavouriteWithAuthUser")
                .WithTags("Wishlist")
                .WithSummary("Add item to favourites for authenticated user")
                .WithDescription("Takes user ID from JWT token and adds an item to the user's wishlist.")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("wishlist/favourite-by-id", async Task<IResult> (
                WishlistCreateDto dto,
                string userId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    await service.Favourite(dto, userId, token);

                    return TypedResults.Ok(new { Message = "Added to favourites successfully." });
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("FavouriteByExplicitUserId")
                .WithTags("Wishlist")
                .WithSummary("Add item to favourites using provided UserId")
                .WithDescription("For admin/service scenarios where the UserId is passed explicitly.")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapGet("wishlist/list", async Task<IResult> (
                HttpContext httpContext,
                Vertical vertical,
                SubVertical subVertical,
                IClassifiedService service,
                AuditLogger auditLogger,
                CancellationToken token) =>
            {
                string? uid = null;
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return Results.Unauthorized();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();

                    if (string.IsNullOrEmpty(uid))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var list = await service.GetAllByUserFavouriteList(uid, vertical, subVertical, token);

                    await auditLogger.LogAuditAsync(
                        module: "Wishlist",
                        httpMethod: "GET",
                        apiEndpoint: "/api/wishlist/list",
                        message: $"Retrieved {list.Count} favourite items.",
                        createdBy: uid,
                        payload: new { Vertical = vertical, SubVertical = subVertical },
                        cancellationToken: token
                    );

                    return TypedResults.Ok(list);
                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Wishlist", "/api/wishlist/list", ex, uid, token);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Wishlist", "/api/wishlist/list", ex, uid, token);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetUserFavouriteList")
                .WithTags("Wishlist")
                .WithSummary("Get favourites for authenticated user")
                .WithDescription("Retrieves the wishlist items for the authenticated user based on their JWT token.")
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();


            group.MapGet("wishlist/list-by-id", async Task<IResult> (
                string userId,
                Vertical vertical,
                SubVertical subVertical,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var list = await service.GetAllByUserFavouriteList(userId, vertical, subVertical, token);

                    return TypedResults.Ok(list);
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetUserFavouriteListById")
                .WithTags("Wishlist")
                .WithSummary("Get favourites using explicit UserId")
                .WithDescription("Retrieves the wishlist items for the specified UserId. Intended for admin/service scenarios.")
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();



            group.MapDelete("wishlist/unfavourite", async Task<IResult> (
                HttpContext httpContext,
                Vertical vertical,
                SubVertical subVertical,
                long adId,
                IClassifiedService service,
                AuditLogger auditLogger,
                CancellationToken token) =>
            {
                string? uid = null;
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return Results.Unauthorized();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();

                    if (string.IsNullOrEmpty(uid))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var message = await service.UnFavourite(uid, vertical, subVertical, adId, token);

                    await auditLogger.LogAuditAsync(
                        module: "Wishlist",
                        httpMethod: "DELETE",
                        apiEndpoint: "/api/wishlist/unfavourite",
                        message: message,
                        createdBy: uid,
                        payload: new { Vertical = vertical, SubVertical = subVertical, AdId = adId },
                        cancellationToken: token
                    );

                    return TypedResults.Ok(new { Message = message });
                }
                catch (ArgumentException ex)
                {
                    await auditLogger.LogExceptionAsync("Wishlist", "/api/wishlist/unfavourite", ex, uid, token);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Wishlist", "/api/wishlist/unfavourite", ex, uid, token);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("UnFavouriteWithAuthUser")
                .WithTags("Wishlist")
                .WithSummary("Remove item from favourites for authenticated user")
                .WithDescription("Takes user ID from JWT token and removes the specified item from the user's wishlist.")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapDelete("wishlist/unfavourite-by-id", async Task<IResult> (
                string userId,
                Vertical vertical,
                SubVertical subVertical,
                long adId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var message = await service.UnFavourite(userId, vertical, subVertical, adId, token);

                    return TypedResults.Ok(new { Message = message });
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
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("UnFavouriteByExplicitUserId")
                .WithTags("Wishlist")
                .WithSummary("Remove item from favourites using provided UserId")
                .WithDescription("For admin/service scenarios where the UserId is passed explicitly.")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();



            #endregion


            group.MapGet("get-category-count", async Task<IResult> (
                 IClassifiedService service,
                 CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetCategoryCountsAsync(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            })
.WithName("GetCategoryCount")
.WithTags("Classified")
.WithSummary("Get category count")
.WithDescription("Get all published ads count based on category.")
.AllowAnonymous()
.Produces<List<SavedSearchResponseDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
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
                    }
                };

                try
                {
                    CommonSearchResponse response = await searchSvc.GetAllAsync(
                        ConstantValues.IndexNames.ClassifiedsItemsIndex,
                        searchReq
                    );

                    var list = response.ClassifiedsItem ?? new List<ClassifiedsItemsIndex>();

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

        public static RouteGroupBuilder MapClassifiedFOStoresEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/stores-search", async (
                [FromBody] ClassifiedsSearchRequest req,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedStoresEndpoints");

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
                        Instance = $"/api/v2/classifiedfo/stores-search"
                    });
                }

                string indexName = ConstantValues.IndexNames.ClassifiedStoresIndex;

                var request = new CommonSearchRequest
                {
                    Text = req.Text,
                    Filters = req.Filters,
                    OrderBy = ""
                };
                if (indexName == null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid SubVertical",
                        Detail = $"Unsupported subVertical value: '{req.SubVertical}'",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/v2/classifiedfo/stores-search"
                    });
                }

                try
                {
                    var results = await svc.GetAllAsync(indexName, request);
                    if (results == null)
                        return Results.NoContent();
                    if (results.ClassifiedStores != null)
                    {
                        var response = new ClassifiedStoreResponse
                        {
                            Stores = results.ClassifiedStores
        .GroupBy(store => new
        {
            store.CompanyId,
            store.SubscriptionId,
            store.CompanyName,
            store.ContactNumber,
            store.Email,
            store.ImageUrl,
            store.BannerUrl,
            store.WebsiteUrl,
            //store.Locations,
            store.StoreSlug
        })
        .Select(group =>
        {
            var firstStore = group.First();
            return new StoresGroup()
            {
                CompanyId = Guid.Parse(group.Key.CompanyId),
                SubscriptionId = Guid.Parse(group.Key.SubscriptionId),
                CompanyName = group.Key.CompanyName,
                ContactNumber = group.Key.ContactNumber,
                Email = group.Key.Email,
                ImageUrl = group.Key.ImageUrl,
                BannerUrl = group.Key.BannerUrl,
                WebsiteUrl = group.Key.WebsiteUrl,
                Locations = firstStore.Locations,
                ProductCount = group.Count(),
                StoreSlug = group.Key.StoreSlug,
                Products = group.Select(g => new ProductInfo
                {
                    ProductId = Guid.Parse(g.ProductId),
                    ProductName = g.ProductName,
                    ProductLogo = g.ProductLogo,
                    ProductPrice = g.ProductPrice,
                    Currency = g.Currency,
                    ProductSummary = g.ProductSummary,
                    ProductDescription = g.ProductDescription,
                    Features = g.Features,
                    Images = g.Images,
                    ProductSlug = g.ProductSlug,
                    ProductCategory = g.ProductCategory,
                    ProductUrl = g.ProductUrl
                }).ToList()
            };
        })
        .ToList()
                        };

                        if (req.OrderBy == "desc")
                        {
                            foreach (var store in response.Stores)
                            {
                                store.Products = store.Products
                                    .OrderByDescending(p => p.ProductPrice)
                                    .ToList();
                            }
                        }
                        else
                        {
                            foreach (var store in response.Stores)
                            {
                                store.Products = store.Products
                                    .OrderBy(p => p.ProductPrice)
                                    .ToList();
                            }
                        }

                        // Pagination
                        int page = Math.Max(1, req.PageNumber);
                        int pageSize = Math.Max(1, Math.Min(100, req.PageSize));
                        var totalCount = response.Stores.Count;
                        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                        if (page > totalPages && totalPages > 0)
                            page = totalPages;

                        var pagedEntities = response.Stores
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                   .ToList();


                        return Results.Ok(new ClassifiedBOPageResponse<StoresGroup>
                        {
                            Page = page,
                            PerPage = pageSize,
                            TotalCount = totalCount,
                            Items = pagedEntities
                        });
                    }
                    else
                    {
                        return Results.NotFound();
                    }

                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid search request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/v2/classifiedfo/stores-search"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception during search");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classifieds/search"
                    );
                }
            })
            .WithName("SearchClassifiedsStores")
            .WithTags("Classified")
            .WithSummary("Classified stores search and products")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapPost("/stores-search-products", async (
                [FromBody] ClassifiedsSearchRequest req,
                [FromQuery] string? ProductName,
                [FromQuery] string? CompanyId,
                [FromServices] ISearchService svc,
                [FromServices] ILoggerFactory logFac
            ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedStoresEndpoints");

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
                        Instance = $"/api/v2/classifiedfo/stores-search-products"
                    });
                }

                string indexName = ConstantValues.IndexNames.ClassifiedStoresIndex;

                var request = new CommonSearchRequest
                {
                    Text = req.Text,
                    Filters = req.Filters,
                    OrderBy = ""
                };
                if (indexName == null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid SubVertical",
                        Detail = $"Unsupported subVertical value: '{req.SubVertical}'",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/v2/classifiedfo/stores-search"
                    });
                }

                try
                {
                    var results = await svc.GetAllAsync(indexName, request);
                    if (results == null)
                        return Results.NoContent();
                    if (results.ClassifiedStores != null)
                    {
                        var response = new ClassifiedStoresProducts
                        {
                            Products = results.ClassifiedStores
                                        .Where(x =>
                                            (string.IsNullOrEmpty(CompanyId) || x.CompanyId == CompanyId) &&
                                             (string.IsNullOrEmpty(ProductName) || x.ProductName.ToLower().Contains(ProductName.ToLower()))

                                        )
                                        .ToList()
                        };

                        response.Products = req.OrderBy?.ToLower() switch
                        {
                            "desc" => response.Products.OrderByDescending(t => t.ProductPrice).ToList(),
                            "asc" => response.Products.OrderBy(t => t.ProductPrice).ToList(),
                            _ => response.Products.OrderBy(t => t.ProductPrice).ToList()
                        };

                        int currentPage = Math.Max(1, req.PageNumber);
                        int itemsPerPage = Math.Max(1, Math.Min(100, req.PageSize));
                        int totalCount = response.Products.Count;
                        int totalPages = (int)Math.Ceiling((double)totalCount / itemsPerPage);

                        if (currentPage > totalPages && totalPages > 0)
                            currentPage = totalPages;

                        var paginated = response.Products
                            .Skip((currentPage - 1) * itemsPerPage)
                            .Take(itemsPerPage)
                            .ToList();


                        return Results.Ok(new ClassifiedBOPageResponse<ClassifiedStoresIndex>
                        {
                            Page = currentPage,
                            PerPage = itemsPerPage,
                            TotalCount = totalCount,
                            Items = paginated
                        });
                    }
                    else
                    {
                        return Results.NotFound();
                    }

                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid search request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/v2/classifiedfo/stores-search-products"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception during search");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classifieds/stores-search-products"
                    );
                }
            })
            .WithName("SearchClassifiedsStoresProducts")
            .WithTags("Classified")
            .WithSummary("Classified stores search products")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapPost("/stores-search-category-list/{StoreSlug}", async Task<IResult> (
    [FromRoute] string? StoreSlug,
    [FromServices] ISearchService svc
) =>
            {
                string indexName = ConstantValues.IndexNames.ClassifiedStoresIndex;

                var request = new CommonSearchRequest
                {
                    Text = "*",
                    Filters = null,
                    //  OrderBy = req.OrderBy,
                    PageNumber = 1,
                    PageSize = 100,

                };
                var results = await svc.GetAllAsync(indexName, request); // You may need to pass indexName/request appropriately

                if (results?.ClassifiedStores == null)
                    return Results.NoContent();

                var categories = results.ClassifiedStores
                    .Where(s => s.StoreSlug == StoreSlug && !string.IsNullOrEmpty(s.ProductCategory))
                    .Select(s => s.ProductCategory)
                    .Distinct()
                    .ToList();

                if (!categories.Any())
                    return Results.NoContent();

                return Results.Ok(new { Categories = categories });

            }).WithName("StoreCategory")
.WithTags("Classified")
.WithSummary("List the categories")
.WithDescription("List the categories from stores")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/stores-search-product-list/{StoreSlug}", async (
                [FromRoute] string? StoreSlug,
       [FromQuery] string? OrderBy,
       [FromQuery] int PageNumber,
       [FromQuery] int PageSize,
       [FromQuery] string? SearchProduct,
       [FromQuery] string? Category,
     [FromServices] ISearchService svc,
     [FromServices] ILoggerFactory logFac
 ) =>
            {

                ClassifiedsSearchRequest req = new ClassifiedsSearchRequest()
                {
                    Text = "*",
                    Filters = new Dictionary<string, object>()
    {
         { "StoreSlug", StoreSlug }
    },
                    OrderBy = OrderBy,
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    SubVertical = "stores"
                };
                var logger = logFac.CreateLogger("ClassifiedStoresEndpoints");

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
                        Instance = $"/api/v2/classifiedfo/stores-search-product-list/{StoreSlug}"
                    });
                }

                string indexName = ConstantValues.IndexNames.ClassifiedStoresIndex;

                var request = new CommonSearchRequest
                {
                    Text = req.Text,
                    Filters = req.Filters
                    //  OrderBy = req.OrderBy,
                    //PageNumber = req.PageNumber,
                    //PageSize = req.PageSize,

                };
                if (indexName == null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid SubVertical",
                        Detail = $"Unsupported subVertical value: '{req.SubVertical}'",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/v2/classifiedfo/stores-search-product-list/{StoreSlug}"
                    });
                }

                try
                {
                    var results = await svc.GetAllAsync(indexName, request);
                    if (results == null)
                        return Results.NoContent();
                    if (results.ClassifiedStores != null)
                    {
                        var response = new ClassifiedStoresProducts
                        {
                            Products = results.ClassifiedStores
                                        .Where(x =>
                                            (string.IsNullOrEmpty(StoreSlug) || x.StoreSlug.ToLower().Contains(StoreSlug.ToLower())) &&
                                            (string.IsNullOrEmpty(SearchProduct) || x.ProductName.ToLower().Contains(SearchProduct.ToLower())) &&
                                            (string.IsNullOrEmpty(Category) || x.ProductCategory.ToLower().Contains(Category.ToLower()))
                                        )
                                        .ToList()
                        };

                        response.Products = OrderBy?.ToLower() switch
                        {
                            "desc" => response.Products.OrderByDescending(t => t.ProductPrice).ToList(),
                            "asc" => response.Products.OrderBy(t => t.ProductPrice).ToList(),
                            _ => response.Products.OrderBy(t => t.ProductPrice).ToList()
                        };

                        // Pagination
                        int page = Math.Max(1, req.PageNumber);
                        int pageSize = Math.Max(1, Math.Min(100, req.PageSize));
                        var totalCount = response.Products.Count;
                        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                        if (page > totalPages && totalPages > 0)
                            page = totalPages;

                        var pagedEntities = response.Products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                   .ToList();


                        return Results.Ok(new ClassifiedBOPageResponse<ClassifiedStoresIndex>
                        {
                            Page = page,
                            PerPage = pageSize,
                            TotalCount = totalCount,
                            Items = pagedEntities
                        });
                    }
                    else
                    {
                        return Results.NotFound();
                    }

                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid search request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/v2/classifiedfo/stores-search-product-list/{StoreSlug}"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception during search");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classifieds//stores-search-product-list/{StoreSlug}"
                    );
                }
            })
           .WithName("SearchClassifiedsStoresProductList")
           .WithTags("Classified")
           .WithSummary("Classified stores search products list")
           .Produces(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapPost("/stores-search-product-details/{ProductSlug}", async (
      [FromRoute] string ProductSlug,
      [FromQuery] string? OrderBy,
      [FromQuery] int PageNumber,
      [FromQuery] int PageSize,
    [FromServices] ISearchService svc,
    [FromServices] ILoggerFactory logFac
) =>
            {

                ClassifiedsSearchRequest req = new ClassifiedsSearchRequest()
                {
                    Text = "*",
                    Filters = new Dictionary<string, object>()
    {
         { "ProductSlug", ProductSlug }
    },
                    OrderBy = OrderBy,
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    SubVertical = "stores"
                };
                var logger = logFac.CreateLogger("ClassifiedStoresEndpoints");

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
                        Instance = $"/api/v2/classifiedfo/stores-search-product-details/{ProductSlug}"
                    });
                }

                string indexName = ConstantValues.IndexNames.ClassifiedStoresIndex;

                var request = new CommonSearchRequest
                {
                    Text = req.Text,
                    Filters = req.Filters,
                    OrderBy = req.OrderBy,
                    PageNumber = req.PageNumber,
                    PageSize = req.PageSize
                };
                if (indexName == null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid SubVertical",
                        Detail = $"Unsupported subVertical value: '{req.SubVertical}'",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/v2/classifiedfo/stores-search-product-details/{ProductSlug}"
                    });
                }

                try
                {
                    var results = await svc.GetAllAsync(indexName, request);
                    if (results == null)
                        return Results.NoContent();
                    if (results.ClassifiedStores != null)
                    {
                        var response = new ClassifiedStoresProducts
                        {
                            Products = results.ClassifiedStores
                                        .Where(x =>

                                             (string.IsNullOrEmpty(ProductSlug) || x.ProductSlug.ToLower().Contains(ProductSlug.ToLower()))
                                        )
                                        .ToList()
                        };

                        response.Products = OrderBy?.ToLower() switch
                        {
                            "desc" => response.Products.OrderByDescending(t => t.ProductPrice).ToList(),
                            "asc" => response.Products.OrderBy(t => t.ProductPrice).ToList(),
                            _ => response.Products.OrderBy(t => t.ProductPrice).ToList()
                        };


                        return Results.Ok(new ClassifiedBOPageResponse<ClassifiedStoresIndex>
                        {
                            Page = req.PageNumber,
                            PerPage = req.PageSize,
                            TotalCount = response.Products.Count,
                            Items = response.Products
                        });
                    }
                    else
                    {
                        return Results.NotFound();
                    }

                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid search request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/v2/classifiedfo/stores-search-product-details/{ProductSlug}"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception during search");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classifieds/stores-search-product-details/{ProductSlug}"
                    );
                }
            })
          .WithName("SearchClassifiedsStoresProductDetails")
          .WithTags("Classified")
          .WithSummary("Classified stores search product details.")
          .Produces(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status204NoContent)
          .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
          .Produces(StatusCodes.Status404NotFound)
          .ProducesProblem(StatusCodes.Status500InternalServerError);


            group.MapGet("/stores-dashboard-header", async Task<Results<
          Ok<List<StoresDashboardHeaderDto>>,
          ForbidHttpResult,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices] IClassifiedsFoService service,
           [FromServices] IV2SubscriptionService subsService,
          HttpContext context,
           string? CompanyId,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {

                    var (userId, userName) = UserTokenHelper.ExtractUserAsync(context);

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.Forbid();
                    }

                    var subscriptions = await subsService.GetActiveSubscriptionsAsync(
                       userId,
                       (int)Vertical.Classifieds,
                       (int)SubVertical.Stores,
                       cancellationToken
                   );

                    if (subscriptions != null && subscriptions.Any())
                    {
                        var result = await service.GetStoresDashboardHeader(userId, CompanyId, cancellationToken);
                        return TypedResults.Ok(result);
                    }
                    else
                    {
                        return TypedResults.Forbid();
                    }


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
                .WithName("StoresDashboardHeader")
                .WithTags("Classified")
                .WithSummary("To display the stores dashboard header information.")
                .WithDescription("Fetches all stores dashboard header information.")
                .Produces<List<StoresDashboardHeaderDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/stores-dashboard-headers", async Task<Results<
              Ok<List<StoresDashboardHeaderDto>>,
              BadRequest<ProblemDetails>,
              ProblemHttpResult>>
              (
              [FromServices] IClassifiedsFoService service,
              HttpContext context,
                string? UserId, string? CompanyId,
              CancellationToken cancellationToken
              ) =>
            {
                try
                {
                    var result = await service.GetStoresDashboardHeader(UserId, CompanyId, cancellationToken);
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
            })
                    .WithName("StoresDashboardHeaders")
                   .ExcludeFromDescription()
                    .WithTags("Classified")
                    .WithSummary("To display the stores dashboard header information.")
                    .WithDescription("Fetches all stores dashboard header information.")
                    .Produces<List<StoresDashboardHeaderDto>>(StatusCodes.Status200OK)
                    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/stores-dashboard-summary", async Task<Results<
          Ok<List<StoresDashboardSummaryDto>>,
           ForbidHttpResult,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices] IClassifiedsFoService service,
           [FromServices] IV2SubscriptionService subsService,
          HttpContext context,
            string? CompanyId, string? SubscriptionId,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    var (userId, userName) = UserTokenHelper.ExtractUserAsync(context);

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.Forbid();
                    }

                    var subscriptions = await subsService.GetActiveSubscriptionsAsync(
                       userId,
                       (int)Vertical.Classifieds,
                       (int)SubVertical.Stores,
                       cancellationToken
                   );

                    if (subscriptions != null && subscriptions.Any())
                    {
                        var result = await service.GetStoresDashboardSummary(CompanyId, SubscriptionId, cancellationToken);
                        return TypedResults.Ok(result);
                    }
                    else
                    {
                        return TypedResults.Forbid();
                    }



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
                .WithName("StoresDashboardSummary")
                .WithTags("Classified")
                .WithSummary("To display the stores dashboard summary information.")
                .WithDescription("Fetches all stores dashboard summary information.")
                .Produces<List<StoresDashboardSummaryDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/stores-dashboard-summarys", async Task<Results<
          Ok<List<StoresDashboardSummaryDto>>,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices] IClassifiedsFoService service,
          HttpContext context,
            string? CompanyId, string? SubscriptionId,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    var result = await service.GetStoresDashboardSummary(CompanyId, SubscriptionId, cancellationToken);
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
            })
                .WithName("StoresDashboardSummarys")
                .ExcludeFromDescription()
                .WithTags("Classified")
                .WithSummary("To display the stores dashboard summary information.")
                .WithDescription("Fetches all stores dashboard summary information.")
                .Produces<List<StoresDashboardSummaryDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/classifiedspaytopromote", async Task<IResult> (
                [FromBody] ClassifiedsPayToPromote request,
                [FromServices] IClassifiedService service,
                [FromServices] HttpContext httpContext,
                [FromServices] AuditLogger auditLogger,
                CancellationToken cancellationToken) =>
            {
                string uid = "unknown";
                string username = "unknown";

                try
                {
                    (uid, username) = UserTokenHelper.ExtractUserAsync(httpContext);


                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        return Results.Problem(
                            detail: "User ID could not be extracted from token.",
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Unauthorized Access");
                    }

                    if (request is null)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ItemsAdId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (request.Vertical != Vertical.Classifieds)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Vertical",
                            Detail = $"Promotion is only supported for {Vertical.Classifieds}. Provided vertical: {request.Vertical}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (request.SubVertical != SubVertical.Items)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid SubVertical",
                            Detail = $"This endpoint only supports {SubVertical.Items}. Provided sub-vertical: {request.SubVertical}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (request.AdId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "AdId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.P2Promote(request, uid, cancellationToken);

                    await auditLogger.LogAuditAsync(
                        module: "Classifieds",
                        httpMethod: "POST",
                        apiEndpoint: "/api/classifieds/paytopromote",
                        message: $"ad {request.AdId} promoted successfully",
                        createdBy: uid,
                        payload: request,
                        cancellationToken: cancellationToken
                    );

                    return Results.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync("Classifieds", "/api/classifieds/paytopromote", ex, uid, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Invalid Request");
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync("Classifieds", "/api/classifieds/paytopromote", ex, uid, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Request");
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classifieds", "/api/classifieds/paytopromote", ex, uid, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
                .WithName("P2Promote")
                .WithTags("Classified")
                .WithSummary("Promote an Classifieds ad")
                .WithDescription("Promotes an Classifieds ad (pay to promote). Requires a valid AdId.")
                .Produces<object>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/classifiedsp2promotebyuserid", async Task<IResult> (
                [FromBody] ClassifiedsPayToPromote request,
                [FromQuery] string uid,
                [FromServices] IClassifiedService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        return Results.Problem(
                            detail: "User id (uid) is required.",
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Invalid Request");
                    }

                    if (request is null || request.AdId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ItemsAdId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.P2Promote(request, uid, cancellationToken);
                    return Results.Ok(result);
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
                .WithName("P2PromoteByUserId")
                .WithTags("Classified")
                .WithSummary("Promote an Classifieds ad (by user id)")
                .WithDescription("Promotes an Classifieds ad by user id. Requires a valid AdId and uid.")
                .Produces<object>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            #region payToFeature with addons for items
            group.MapPut("/classifiedsfeature", async Task<IResult> (
    HttpContext httpContext,
    ClassifiedsPayToFeature dto,
    AuditLogger auditLogger,
    IClassifiedService service,
    CancellationToken token) =>
            {
                string uid = "unknown";
                string username = "unknown";

                try
                {
                    (uid, username) = UserTokenHelper.ExtractUserAsync(httpContext);

                    if (uid == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Subscription Required",
                            Detail = "No valid subscription found for this user.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    await service.P2PFeature(dto, uid, dto.AddonId, token);

                    await auditLogger.LogAuditAsync(
                        module: "Classified",
                        httpMethod: "PUT",
                        apiEndpoint: "/api/classifieds/items/feature",
                        message: $"Featured classified ad with ID {dto.AdId}",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: token
                    );

                    return TypedResults.Ok(new
                    {
                        AdId = dto.AdId,
                        Message = "The ad has been successfully marked as featured."
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classified", "/api/classifieds/items/feature", ex, uid, token);

                    return ex switch
                    {
                        ArgumentException => TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        }),
                        InvalidOperationException => TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        }),
                        KeyNotFoundException => TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        }),
                        _ => TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError
                        )
                    };
                }
            })
.RequireAuthorization()
    .WithName("ClassifiedsFeature")
    .WithTags("Classified")
    .WithSummary("Feature the ad (public)")
    .WithDescription("Marks the ad as featured after validating subscription from user claims.")
    .Produces(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/classifiedsfeaturedid", async Task<IResult> (
                [FromQuery] int adId,
                [FromQuery] int subVertical,
                [FromQuery] string userId,
                [FromQuery] Guid addonId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (adId <= 0)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "AdId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var dto = new ClassifiedsPayToFeature { AdId = adId, SubVertical = (SubVertical)subVertical };

                    await service.P2PFeature(dto, userId, addonId, token);

                    return TypedResults.Ok(new
                    {
                        AdId = adId,
                        Message = $"The ad has been successfully featured in subVertical {subVertical}."
                    });
                }
                catch (Exception ex)
                {
                    return ex switch
                    {
                        ArgumentException => TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        }),
                        InvalidOperationException => TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        }),
                        KeyNotFoundException => TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        }),
                        _ => TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError
                        )
                    };
                }
            })
            .ExcludeFromDescription()
    .WithName("FeatureInternalAd")
    .WithTags("Classified")
    .WithSummary("Feature the ad (internal)")
    .WithDescription("Endpoint used by external service to feature an ad directly.")
    .Produces(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);





            group.MapPost("/classifiedspaytopublish", async Task<IResult> (
                [FromBody] ClassifiedsPayToPublish request,
                [FromServices] IClassifiedService service,
                [FromServices] HttpContext httpContext,
                [FromServices] AuditLogger auditLogger,
                CancellationToken cancellationToken) =>
            {
                string uid = "unknown";
                string username = "unknown";

                try
                {
                    (uid, username) = UserTokenHelper.ExtractUserAsync(httpContext);

                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        return Results.Problem(
                            detail: "User ID could not be extracted from token.",
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Unauthorized Access");
                    }

                    if (request is null)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. AdId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (request.AdId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "AdId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.P2Publish(request, uid, cancellationToken);

                    await auditLogger.LogAuditAsync(
                        module: "Classifieds",
                        httpMethod: "POST",
                        apiEndpoint: "/api/classifieds/paytopublish",
                        message: $"Ad {request.AdId} published successfully",
                        createdBy: uid,
                        payload: request,
                        cancellationToken: cancellationToken
                    );

                    return Results.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync("Classifieds", "/api/classifieds/paytopublish", ex, uid, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Invalid Request");
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync("Classifieds", "/api/classifieds/paytopublish", ex, uid, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Request");
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync("Classifieds", "/api/classifieds/paytopublish", ex, uid, cancellationToken);
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
                .WithName("P2Publish")
                .WithTags("Classified")
                .WithSummary("Publish a Classifieds ad")
                .WithDescription("Publishes a Classifieds ad (pay to publish). Requires a valid AdId.")
                .Produces<object>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/classifiedspaytopublishbyuserid", async Task<IResult> (
                [FromBody] ClassifiedsPayToPublish request,
                [FromQuery] string uid,
                [FromServices] IClassifiedService service,
                CancellationToken cancellationToken) =>
            {
                try
                {

                    if (string.IsNullOrWhiteSpace(uid))
                    {
                        return Results.Problem(
                            detail: "User id (uid) is required.",
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Invalid Request");
                    }

                    if (request is null || request.AdId <= 0)
                    {
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Request",
                            Detail = "Invalid request data. ItemsAdId must be a positive number.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }


                    var result = await service.P2Publish(request, uid, cancellationToken);
                    return Results.Ok(result);
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
                .WithName("P2PublishByUserId")
                .WithTags("Classified")
                .WithSummary("Publish an Classifieds ad (by user id)")
                .WithDescription("Publishes an Classifieds ad by user id. Requires a valid AdId and uid.")
                .Produces<object>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/stores-dashboard-process-csv",
   async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ForbidHttpResult, ProblemHttpResult>> (
       string Url,
       string CsvPlatform,
       string CompanyId,
       string SubscriptionId,
       string Domain,
       [FromServices] IClassifiedsFoService service,
       HttpContext context,
       CancellationToken cancellationToken
   ) =>
   {
       try
       {
           var (userId, error) = GenericClaimsHelper.GetValidUserId(context.User);
           if (!string.IsNullOrEmpty(error))
           {
               return TypedResults.Problem(
               title: "Subscription issue in token.",
               detail: error,
               statusCode: StatusCodes.Status500InternalServerError,
               instance: context.Request.Path
               );
           }

           if (string.IsNullOrEmpty(userId))
           {
               return TypedResults.Forbid();
           }

           var result = await service.GetFOProcessStoresCSV(Url, CsvPlatform, CompanyId, SubscriptionId, userId?.ToString(), Domain, cancellationToken);

           switch (result?.ToString())
           {
               case "created":
                   return TypedResults.Ok("Products have been successfully created at the specified store(s).");

               case "No products":
                   return TypedResults.BadRequest(new ProblemDetails
                   {
                       Title = "No Products Found",
                       Detail = "The CSV did not contain any valid products to process.",
                       Status = StatusCodes.Status400BadRequest
                   });

               case "Insufficient quota":
                   return TypedResults.BadRequest(new ProblemDetails
                   {
                       Title = "Insufficient quota",
                       Detail = "The CSV did not contain any valid quota to process.",
                       Status = StatusCodes.Status400BadRequest
                   });

               case "Fail to reserve quota":
                   return TypedResults.BadRequest(new ProblemDetails
                   {
                       Title = "Fail to reserve quota",
                       Detail = "The CSV fail to reserve quota to process.",
                       Status = StatusCodes.Status400BadRequest
                   });

               default:
                   return TypedResults.BadRequest(new ProblemDetails
                   {
                       Title = "Store Processing Failed",
                       Detail = result?.ToString() ?? "Unknown error occurred.",
                       Status = StatusCodes.Status400BadRequest
                   });
           }
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
               .WithName("GetDashboardProcessStoresCSV")
               .WithTags("Classified")
               .WithSummary("Process the csv file.")
               .WithDescription("Processing the uploaded csv.Storing the products into data layer.")
               .Produces<string>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status403Forbidden)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/stores-dashboard-processing-csv",
   async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ForbidHttpResult, ProblemHttpResult>> (
       string Url,
        string CsvPlatform,
       string CompanyId,
       string SubscriptionId,
       string UserId,
       string Domain,
       [FromServices] IClassifiedsFoService service,
       HttpContext context,
       CancellationToken cancellationToken
   ) =>
   {
       try
       {
           var result = await service.GetFOProcessStoresCSV(Url, CsvPlatform, CompanyId, SubscriptionId, UserId, Domain, cancellationToken);
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
   })
               .ExcludeFromDescription()
               //.AllowAnonymous()
               .WithName("GetDashboardProcessStoreCSV")
               .WithTags("Classified")
               .WithSummary("Processing the csv.")
               .WithDescription("Processing the uploaded csv.Storing the products into data layer.")
               .Produces<string>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status403Forbidden)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;


            #endregion

        }

    }
}