using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using Microsoft.AspNetCore.Builder;
using QLN.Common.Infrastructure.Utilities;
using System.Text.Json;
using QLN.Common.Infrastructure.DTO_s;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.CustomException;

public static class V2NewsEndpoints
{
    public static RouteGroupBuilder MapCreateNewsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/writertags", async Task<Results<
         Ok<WriterTagsResponse>,
         ProblemHttpResult>>
         (
             IV2NewsService service,
             CancellationToken cancellationToken
         ) =>
        {
            try
            {
                var tags = await service.GetWriterTagsAsync(cancellationToken);
                return TypedResults.Ok(tags);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Error retrieving writer tags", ex.Message);
            }
        })
         .WithName("getWriterTags")
         .WithTags("News")
         .WithSummary("Get all writer tags as key-value JSON")
         .WithDescription("Returns writer tags in a key-value JSON object format")
         .Produces<Dictionary<string, string>>(StatusCodes.Status200OK)
         .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/slots", async Task<Results<Ok<List<V2NewsSlot>>, ProblemHttpResult>> (
        IV2NewsService slotService,
        CancellationToken cancellationToken
        ) =>
                {
                    try
                    {
                        var result = await slotService.GetAllSlotsAsync(cancellationToken);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem($"Unexpected error: {ex.Message}");
                    }
                })
        .WithName("GetAllSlots")
        .WithTags("News")
        .WithSummary("Get All Slot Options")
        .WithDescription("Returns a list of all slot enum values and names.")
        .Produces<List<V2NewsSlot>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/filterbystatus", async Task<Results<Ok<List<V2NewsArticleDTO>>, ProblemHttpResult>> (
        [FromQuery] bool? isActive,
        IV2NewsService service,
        CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.GetAllNewsFilterArticles(isActive, cancellationToken);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Error fetching articles: " + ex.Message);
            }
        })
    .WithName("GetFilteredNewsArticles")
    .WithTags("News")
    .WithSummary("Get filtered news articles")
    .WithDescription("Returns active or inactive news articles based on the provided isActive flag.")
    .Produces<List<V2NewsArticleDTO>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/news", async Task<Results<
          Ok<string>,
          ForbidHttpResult,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
      (
          V2NewsArticleDTO dto,
          IV2NewsService service,
          HttpContext httpContext,
          CancellationToken cancellationToken
      ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Forbid();
                    }
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    if (uid == null)
                    {
                        return TypedResults.Forbid();
                    }
                    var name = userData.GetProperty("name").GetString();

                    dto.CreatedBy = uid;
                    dto.UpdatedBy = uid;
                    dto.CreatedAt = DateTime.UtcNow;
                    dto.UpdatedAt = DateTime.UtcNow;
                    dto.authorName = name;

                    var result = await service.CreateNewsArticleAsync(uid, dto, cancellationToken);
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
            .RequireAuthorization()
      .WithName("CreateNewsArticle")
      .WithTags("News")
      .WithSummary("Create News Article")
      .WithDescription("Creates a news article and returns only success message.")
      .Produces<string>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/createNewsArticleById", async Task<Results<
             Ok<string>,
             BadRequest<ProblemDetails>,
             ProblemHttpResult>>
         (
             V2NewsArticleDTO dto,
             IV2NewsService service,
             CancellationToken cancellationToken
         ) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(dto.CreatedBy) || string.IsNullOrWhiteSpace(dto.UpdatedBy))
                        {
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Validation Error",
                                Detail = "CreatedBy and UpdatedBy must be provided in the payload.",
                                Status = StatusCodes.Status400BadRequest
                            });
                        }

                        dto.CreatedAt = DateTime.UtcNow;
                        dto.UpdatedAt = DateTime.UtcNow;

                        var result = await service.CreateNewsArticleAsync(dto.CreatedBy, dto, cancellationToken);
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
         .ExcludeFromDescription()
         .WithName("CreateNewsArticleByUserId")
         .WithTags("News")
         .WithSummary("Create News Article By UserId")
         .WithDescription("Creates a news article using CreatedBy and UpdatedBy passed explicitly.")
         .Produces<string>(StatusCodes.Status200OK)
         .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
         .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapGet("/news", async Task<Results<
    Ok<PagedResponse<V2NewsArticleDTO>>,
    NotFound<ProblemDetails>,
    ProblemHttpResult>>
(
    [FromQuery] int? page,
    [FromQuery] int? perPage,
    [FromQuery] string? search,
    IV2NewsService service,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                var result = await service.GetAllNewsArticlesAsync(page, perPage, search, cancellationToken);

                if (result == null || result.Items == null || !result.Items.Any())
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "No Articles Found",
                        Detail = "No news articles match the provided search.",
                        Status = StatusCodes.Status404NotFound
                    });
                }
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem(new ProblemDetails
                {
                    Title = "Server Error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        })
.WithName("GetPaginatedNewsArticles")
.WithTags("News")
.WithSummary("Paginated News Articles List (title search)")
.WithDescription("Fetches news articles with optional title search and pagination.")
.Produces<PagedResponse<V2NewsArticleDTO>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



        group.MapGet("/categories/{categoryId:int}", async Task<Results<Ok<List<V2NewsArticleDTO>>, ProblemHttpResult>> (
            int categoryId,
            IV2NewsService service,
            CancellationToken cancellationToken
        ) =>
                {
                    try
                    {
                        var result = await service.GetArticlesByCategoryIdAsync(categoryId, cancellationToken);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem("Internal Server Error", ex.Message);
                    }
                })
        .WithName("GetArticlesByCategory")
        .WithTags("News")
        .Produces<List<V2NewsArticleDTO>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/categories/{categoryId}/sub/{subCategoryId}", async Task<Results<
              Ok<List<V2NewsArticleDTO>>,
              BadRequest<ProblemDetails>,
              NotFound<ProblemDetails>,
              ProblemHttpResult>>
          (
              int categoryId,
              int subCategoryId,
              string? status,
              int? page,
              int? pageSize,
              IV2NewsService service,
              CancellationToken cancellationToken
          ) =>
        {
            try
            {
                if (categoryId <= 0 || subCategoryId <= 0)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid request",
                        Detail = "Both categoryId and subCategoryId must be greater than zero"
                    });
                }

                var articles = await service.GetArticlesBySubCategoryIdAsync(
                    categoryId,
                    subCategoryId,
                    status,
                    page,
                    pageSize,
                    cancellationToken);

                if (articles == null)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "No articles found",
                        Detail = $"No articles found for category {categoryId}, subcategory {subCategoryId}"
                    });
                }

                return TypedResults.Ok(articles);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem(new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = ex.Message
                });
            }
           

        })
        .WithName("GetArticlesBySubCategory")
        .WithTags("News")
        .Produces<List<V2NewsArticleDTO>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);





        group.MapPut("/updatenews", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>>
        (
            V2NewsArticleDTO dto,
            IV2NewsService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
        ) =>
               {
                   try
                   {
                       var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                       if (string.IsNullOrEmpty(userClaim))
                       {
                           return TypedResults.Forbid();
                       }
                       var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                       var uid = userData.GetProperty("uid").GetString();
                       var authorName = userData.GetProperty("name").GetString()??"";
                       if (uid == null)
                       {
                           return TypedResults.Forbid();
                       }
                       dto.UpdatedBy = uid;

                       if (dto.Id == Guid.Empty)
                           return TypedResults.BadRequest(new ProblemDetails
                           {
                               Title = "Validation Error",
                               Detail = "News ID must be provided.",
                               Status = 400
                           });

                       dto.UserId = uid;
                       dto.authorName = authorName;
                       dto.UpdatedBy = uid;
                       dto.UpdatedAt = DateTime.UtcNow;

                       var result = await service.UpdateNewsArticleAsync(dto, cancellationToken);

                       return TypedResults.Ok(result); 
                   }
                   catch (KeyNotFoundException ex)
                   {
                       return TypedResults.NotFound(new ProblemDetails
                       {
                           Title = "Not Found",
                           Detail = ex.Message,
                           Status = 404
                       });
                   }
                   catch (InvalidDataException ex)
                   {
                       return TypedResults.BadRequest(new ProblemDetails
                       {
                           Title = "Invalid Data",
                           Detail = ex.Message,
                           Status = 400
                       });
                   }
                   catch (Exception ex)
                   {
                       return TypedResults.Problem("Internal Server Error", ex.Message);
                   }
               })
            .RequireAuthorization()
        .WithName("UpdateNewsArticles")
        .WithTags("News")
        .WithSummary("Update News (Authenticated)")
        .WithDescription("Updates a news entry using DTO and assigns the user info from access token.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapPut("/updateNewsarticleByUserId", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ProblemHttpResult>>
        (
            V2NewsArticleDTO dto,
            IV2NewsService service,
            CancellationToken cancellationToken
        ) =>
                    {
                        try
                        {
                            if (dto.UserId == string.Empty || string.IsNullOrWhiteSpace(dto.authorName))
                            {
                                return TypedResults.BadRequest(new ProblemDetails
                                {
                                    Title = "Validation Error",
                                    Detail = "UserId and authorName must be provided.",
                                    Status = 400
                                });
                            }

                            var result = await service.UpdateNewsArticleAsync(dto, cancellationToken);
                            return TypedResults.Ok(result);
                        }
                        catch (KeyNotFoundException ex)
                        {
                            return TypedResults.NotFound(new ProblemDetails
                            {
                                Title = "Not Found",
                                Detail = ex.Message,
                                Status = 404
                            });
                        }
                        catch (InvalidDataException ex)
                        {
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Invalid Data",
                                Detail = ex.Message,
                                Status = 400
                            });
                        }
                        catch (Exception ex)
                        {
                            return TypedResults.Problem("Internal Server Error", ex.Message);
                        }
                    })
        .WithName("UpdateArtcleNewsByUserId")
        .WithTags("News")
        .WithSummary("Update News Article")
        .WithDescription("Updates a news article based on provided payload without using ID in URL.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .ExcludeFromDescription();

        group.MapDelete("/news/{id:guid}",
            async Task<Results<
                Ok<string>,
                NotFound<ProblemDetails>,
                Conflict<ProblemDetails>,
                ProblemHttpResult>>
            (
                [FromRoute] Guid id,
                [FromServices] IV2NewsService service,
                [FromServices] ILogger<IV2NewsService> log,
                CancellationToken ct
            ) =>
            {
                try
                {
                    var msg = await service.DeleteNews(id, ct);
                    return TypedResults.Ok(msg);
                }
                catch (KeyNotFoundException knf)
                {
                    log.LogWarning(knf, "News {NewsId} not found", id);
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = knf.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (InvalidOperationException iex)
                {
                    log.LogWarning(iex, "Cannot delete News {NewsId}", id);
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict – cannot delete",
                        Detail = iex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (DaprServiceException dse)
                {
                    log.LogError(dse, "Upstream error deleting News {NewsId}", id);
                    return TypedResults.Problem(
                    title:   "Upstream service error",
                    detail:  dse.ResponseBody,
                    statusCode: dse.StatusCode,
                    instance: $"/news/{id}");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Unhandled exception deleting News {NewsId}", id);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("DeleteNews")
            .WithTags("News")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status502BadGateway)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/reorderslot", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>>
        (
            NewsSlotReorderRequest dto,
            IV2NewsService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
                if (dto.SlotAssignments == null || dto.SlotAssignments.Count != 13)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Slot values must be between 1 and 13.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (string.IsNullOrEmpty(userClaim))
                {
                    return TypedResults.Forbid();
                }
                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var uid = userData.GetProperty("uid").GetString();
                if (uid == null)
                {
                    return TypedResults.Forbid();
                }
                var name = userData.GetProperty("name").GetString();


                var result = await service.ReorderSlotsAsync(dto, cancellationToken);
                return TypedResults.Ok(result);
            }
            catch (InvalidDataException ex)
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
                return TypedResults.Problem("Internal Server Error", ex.Message);
            }
        })
            .RequireAuthorization()
        .WithName("ReorderLiveSlots")
        .WithTags("News")
        .WithSummary("Reorder Live Slots (Authenticated)")
        .WithDescription("Reorders live news articles using authenticated user.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/reorderLiveSlotsByUserId", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                NewsSlotReorderRequest dto,
                IV2NewsService service,
                CancellationToken cancellationToken
            ) =>
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(dto.UserId))
                            {
                                return TypedResults.BadRequest(new ProblemDetails
                                {
                                    Title = "Validation Error",
                                    Detail = "UserId must be provided.",
                                    Status = StatusCodes.Status400BadRequest
                                });
                            }

                            if (dto.SlotAssignments == null || dto.SlotAssignments.Count != 13)
                            {
                                return TypedResults.BadRequest(new ProblemDetails
                                {
                                    Title = "Validation Error",
                                    Detail = "Slot values must be between 1 and 13.",
                                    Status = StatusCodes.Status400BadRequest
                                });
                            }

                            var result = await service.ReorderSlotsAsync(dto, cancellationToken);
                            return TypedResults.Ok(result);
                        }
                        catch (InvalidDataException ex)
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
                            return TypedResults.Problem("Internal Server Error", ex.Message);
                        }
                    })
            .ExcludeFromDescription()
            .WithName("ReorderLiveSlotsByUserId")
            .WithTags("News")
            .WithSummary("Reorder Live Slots (Manual/UserId)")
            .WithDescription("Reorders live news slots using UserId from the payload.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/getbyid/{id:guid}", async Task<Results<
            Ok<V2NewsArticleDTO>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>>
        (
            Guid id,
            IV2NewsService service,
            CancellationToken cancellationToken
        ) =>
                {
                    try
                    {
                        var article = await service.GetArticleByIdAsync(id, cancellationToken);
                        if (article is null)
                        {
                            return TypedResults.NotFound(new ProblemDetails
                            {
                                Title = "Not Found",
                                Detail = $"No article found with ID: {id}",
                                Status = StatusCodes.Status404NotFound
                            });
                        }

                        return TypedResults.Ok(article);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem("Internal Server Error", ex.Message);
                    }
                })
        .WithName("GetNewsArticleById")
        .WithTags("News")
        .WithSummary("Get News Article by ID")
        .WithDescription("Returns the news article for the given ID.")
        .Produces<V2NewsArticleDTO>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/getbyslug/{slug}", async Task<Results<
    Ok<V2NewsArticleDTO>,
    NotFound<ProblemDetails>,
    ProblemHttpResult>>
(
    string slug,
    IV2NewsService service,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                var article = await service.GetArticleBySlugAsync(slug, cancellationToken);
                if (article is null)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = $"No article found with slug: {slug}",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                return TypedResults.Ok(article);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message);
            }
        })
.WithName("GetNewsArticleBySlug")
.WithTags("News")
.WithSummary("Get News Article by Slug")
.WithDescription("Returns the news article for the provided slug.")
.Produces<V2NewsArticleDTO>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/createcategory", async Task<Results<
    Ok<string>,
    ForbidHttpResult,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    V2NewsCategory category,
    IV2NewsService service,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (userClaim == null)
                {
                    return TypedResults.Forbid();
                }

                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var uid = userData.GetProperty("uid").GetString();
                if (uid == null)
                {
                    return TypedResults.Forbid();
                }
                var name = userData.GetProperty("name").GetString();

                if (string.IsNullOrWhiteSpace(category.CategoryName))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "CategoryName is required."
                    });
                }

                category.Id = category.Id <= 0 ? 101 : category.Id;
                category.SubCategories ??= [];

                var subId = 1001;
                foreach (var sub in category.SubCategories)
                {
                    sub.Id = sub.Id <= 0 ? subId++ : sub.Id;
                }

                await service.AddCategoryAsync(category, cancellationToken);

                return TypedResults.Ok("News category created successfully.");
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to create news category", ex.Message);
            }
        })
            .RequireAuthorization()
.WithName("CreateNewsCategory")
.WithTags("News")
.WithSummary("Create a news category (Authorized)")
.WithDescription("Creates a category by authenticated user with subcategories")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapPost("/category/createById", async Task<Results<
    Ok<string>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    V2NewsCategory category,
    IV2NewsService service,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category.CategoryName))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "CategoryName is required."
                    });
                }

                category.Id = category.Id <= 0 ? 101 : category.Id;
                category.SubCategories ??= [];

                var subId = 1001;
                foreach (var sub in category.SubCategories)
                {
                    sub.Id = sub.Id <= 0 ? subId++ : sub.Id;
                }

                await service.AddCategoryAsync(category, cancellationToken);

                return TypedResults.Ok("News category created successfully.");
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to create news category", ex.Message);
            }
        })
.ExcludeFromDescription()
.WithName("CreateNewsCategoryById")
.WithTags("News")
.WithSummary("Create a news category by explicit ID (no auth)")
.WithDescription("Creates a category and subcategories using payload-provided data. No authorization required.")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/allcategories", async Task<Results<
    Ok<List<V2NewsCategory>>,
    ProblemHttpResult>>
(
    IV2NewsService service,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                var categories = await service.GetAllCategoriesAsync(cancellationToken);
                return TypedResults.Ok(categories);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to retrieve categories", ex.Message);
            }
        })
.WithName("GetAllCategories")
.WithTags("News")
.WithSummary("Get all news categories")
.Produces<List<V2NewsCategory>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapGet("/categorygetbyid/{id:int}", async Task<Results<
    Ok<V2NewsCategory>,
    NotFound<ProblemDetails>,
    ProblemHttpResult>>
(
    int id,
    IV2NewsService service,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                var category = await service.GetCategoryByIdAsync(id, cancellationToken);
                return category != null
                    ? TypedResults.Ok(category)
                    : TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = $"Category with ID {id} was not found."
                    });
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Error retrieving category", ex.Message);
            }
        })
.WithName("GetCategoryById")
.WithTags("News")
.WithSummary("Get news category by ID")
.Produces<V2NewsCategory>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapPut("/category/updatesubcategory", async Task<Results<
    Ok<string>,
    BadRequest<ProblemDetails>,
    NotFound<ProblemDetails>,
    ForbidHttpResult,
    ProblemHttpResult>>
(
    int categoryId,
    V2NewsSubCategory subcategory,
    IV2NewsService service,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (userClaim == null)
                {
                    return TypedResults.Forbid();
                }

                if (categoryId <= 0 || subcategory.Id <= 0 || string.IsNullOrWhiteSpace(subcategory.SubCategoryName))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "CategoryId, SubCategoryId, and SubCategoryName are required."
                    });
                }

                var updated = await service.UpdateSubCategoryAsync(categoryId, subcategory, cancellationToken);

                return updated
                    ? TypedResults.Ok("Subcategory updated successfully.")
                    : TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "Subcategory not found in specified category."
                    });
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Error updating subcategory", ex.Message);
            }
        })
            .RequireAuthorization()
.WithName("UpdateSubCategory")
.WithTags("News")
.WithSummary("Update subcategory name by authorized user")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapPut("/category/subcategorybyid", async Task<Results<
    Ok<string>,
    BadRequest<ProblemDetails>,
    NotFound<ProblemDetails>,
    ProblemHttpResult>>
(
    int categoryId,
    V2NewsSubCategory subcategory,
    IV2NewsService service,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                if (categoryId <= 0 || subcategory.Id <= 0 || string.IsNullOrWhiteSpace(subcategory.SubCategoryName))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "CategoryId, SubCategoryId, and SubCategoryName are required."
                    });
                }

                var updated = await service.UpdateSubCategoryAsync(categoryId, subcategory, cancellationToken);

                return updated
                    ? TypedResults.Ok("Subcategory updated successfully.")
                    : TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "Subcategory not found in specified category."
                    });
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Error updating subcategory", ex.Message);
            }
        })
.ExcludeFromDescription()
.WithName("UpdateSubCategoryById")
.WithTags("News")
.WithSummary("Update subcategory without authorization")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/comments", async Task<Results<
    Ok<NewsCommentApiResponse>,
    ForbidHttpResult,
    ProblemHttpResult>>
(
    V2NewsCommentDto dto,
    IV2NewsService service,
    HttpContext httpContext,
    CancellationToken ct
) =>
        {
            try
            {
                var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (string.IsNullOrEmpty(userClaim))
                    return TypedResults.Forbid();

                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);

                dto.Uid = userData.GetProperty("uid").GetString();
                if (dto.Uid == null)
                {
                    return TypedResults.Forbid();
                }
                dto.UserName = userData.GetProperty("name").GetString();
                dto.CommentedAt = DateTime.UtcNow;
                dto.CommentId = Guid.NewGuid();

                var response = await service.SaveNewsCommentAsync(dto, ct);
                return TypedResults.Ok(response);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to post news comment.", ex.Message);
            }
        })
            .RequireAuthorization()
.WithName("PostNewsComment")
.WithTags("News")
.WithSummary("Post comment for a news article (JWT based)")
.WithDescription("Takes the user ID from JWT token and posts a comment for the given article ID.")
.Produces<NewsCommentApiResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status403Forbidden)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapPost("/commentsavebyid", async Task<Results<
    Ok<NewsCommentApiResponse>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    V2NewsCommentDto dto,
    IV2NewsService service,
    CancellationToken ct
) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Uid))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Missing User ID",
                        Detail = "User ID (Uid) is required in the payload.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                dto.CommentedAt = DateTime.UtcNow;
                dto.CommentId = Guid.NewGuid();

                var response = await service.SaveNewsCommentAsync(dto, ct);
                return TypedResults.Ok(response);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to post news comment (by user ID).", ex.Message);
            }
        })
.ExcludeFromDescription()
.WithName("PostNewsCommentByUserId")
.WithTags("News")
.WithSummary("Post comment with explicit user ID")
.WithDescription("Used when the client/service sends the User ID directly in the request payload.")
.Produces<NewsCommentApiResponse>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapGet("/commentsbyArticleid/{nid}", async Task<Results<
     Ok<NewsCommentListResponse>,
     NotFound<ProblemDetails>,
     ProblemHttpResult>> (
     string nid,
     int? page,
     int? perPage,
     IV2NewsService service,
     CancellationToken ct
 ) =>
        {
            try
            {
                var response = await service.GetCommentsByArticleIdAsync(nid, page, perPage, ct);

                // Always return 200 OK, even if comments are empty
                return TypedResults.Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Article or Comment Index Not Found",
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                return TypedResults.Problem(new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "Error retrieving comments",
                    Status = 500,
                    Instance = $"Error retrieving comments for article {nid}"
                });
            }
        })
 .WithName("GetCommentsByArticleId")
 .WithTags("News")
 .WithSummary("Get all comments for a specific news article")
 .WithDescription("Returns a paginated list of comments for the provided article ID. Pagination is optional.")
 .Produces<NewsCommentListResponse>(StatusCodes.Status200OK)
 .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
 .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



        group.MapPost("/commentslike/{commentId}", async Task<Results<
         Ok<bool>,
         ForbidHttpResult,
         ProblemHttpResult>>
     (
         string commentId,
         IV2NewsService service,
         HttpContext httpContext,
         CancellationToken ct
     ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                        return TypedResults.Forbid();

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var userId = userData.GetProperty("uid").GetString();                    
                    if (string.IsNullOrWhiteSpace(userId))
                        return TypedResults.Forbid();

                    var userName = userData.GetProperty("name").GetString();
                    if(string.IsNullOrWhiteSpace(userName))
                        return TypedResults.Forbid();


                    var result = await service.LikeNewsCommentAsync(commentId, userId, userName, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Failed to toggle like for news comment.", ex.Message);
                }
            })
        .RequireAuthorization()    
     .WithName("LikeNewsCommentJWT")
     .WithTags("News")
     .WithSummary("Toggle like on a comment (JWT-based)")
     .WithDescription("Toggles like/unlike for a news comment by reading user ID from JWT token.")
     .Produces<bool>(StatusCodes.Status200OK)
     .Produces(StatusCodes.Status403Forbidden)
     .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/commentsbyid/{commentId}", async Task<Results<
            Ok<bool>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            string commentId,
            [FromQuery] string userId,
            [FromQuery] string userName,
            IV2NewsService service,
            CancellationToken ct
        ) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Missing User ID",
                        Detail = "The 'userId' query parameter is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Missing User Name",
                        Detail = "The 'userName' query parameter is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var result = await service.LikeNewsCommentAsync(commentId, userId, userName, ct);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to toggle like (by user ID).", ex.Message);
            }
        })
        .ExcludeFromDescription()
        .WithName("LikeNewsCommentByUserId")
        .WithTags("News")
        .WithSummary("Toggle like with explicit user ID")
        .WithDescription("Used when the client provides the user ID directly in query (not via JWT).")
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);       

        group.MapPost("/comments/delete/{articleId}/{commentId}", async Task<Results<
            Ok<NewsCommentApiResponse>,
            ForbidHttpResult,
            ProblemHttpResult>>
            (
            string articleId,
            Guid commentId,
            IV2NewsService service,
            HttpContext httpContext,
            CancellationToken ct
            ) =>
        {
            try
            {
                var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (string.IsNullOrWhiteSpace(userClaim))
                    return TypedResults.Forbid();

                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var userId = userData.GetProperty("uid").GetString();
                if (string.IsNullOrWhiteSpace(userId))
                    return TypedResults.Forbid();

                var result = await service.SoftDeleteNewsCommentAsync(articleId, commentId, userId, ct);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to delete comment using JWT.", ex.Message);
            }
        })
            .WithName("SoftDeleteNewsCommentJWT")
            .WithTags("News")
            .WithSummary("Soft delete a comment using JWT")
            .WithDescription("Sets IsActive=false for a comment. Only the owner can delete their own comment or reply using JWT.")
            .Produces<NewsCommentApiResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/comments/delete/byid/{articleId}/{commentId}", async Task<Results<
            Ok<NewsCommentApiResponse>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            string articleId,
            Guid commentId,
            [FromQuery] string userId,
            IV2NewsService service,
            CancellationToken ct
            ) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Missing User ID",
                        Detail = "The 'userId' query parameter is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var result = await service.SoftDeleteNewsCommentAsync(articleId, commentId, userId, ct);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to delete comment with provided user ID.", ex.Message);
            }
        })
            .ExcludeFromDescription()
            .WithName("SoftDeleteNewsCommentByUserId")
            .WithTags("News")
            .WithSummary("Soft delete a comment by ID (explicit userId)")
            .WithDescription("Used for admin/debug cases to delete a comment by supplying the userId directly.")
            .Produces<NewsCommentApiResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/comments/edit/{articleId}/{commentId}", async Task<Results<
            Ok<NewsCommentApiResponse>,
            ForbidHttpResult,
            ProblemHttpResult>>
            (
            string articleId,
            Guid commentId,
            [FromBody] string updatedText,
            IV2NewsService service,
            HttpContext httpContext,
            CancellationToken ct
            ) =>
        {
            try
            {
                var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                if (string.IsNullOrEmpty(userClaim))
                    return TypedResults.Forbid();

                var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                var userId = userData.GetProperty("uid").GetString();
                if (string.IsNullOrWhiteSpace(userId))
                    return TypedResults.Forbid();

                var result = await service.EditNewsCommentAsync(articleId, commentId, userId, updatedText, ct);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to edit news comment.", ex.Message);
            }
        })
            .WithName("EditNewsCommentJWT")
            .WithTags("News")
            .WithSummary("Edit a news comment (JWT-based)")
            .WithDescription("Allows a user to edit their comment by reading user ID from JWT token.")
            .Produces<NewsCommentApiResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/comments/editbyid/{articleId}/{commentId}", async Task<Results<
            Ok<NewsCommentApiResponse>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            string articleId,
            Guid commentId,
            [FromQuery] string userId,
            [FromBody] string updatedText,
            IV2NewsService service,
            CancellationToken ct
            ) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Missing User ID",
                        Detail = "The 'userId' query parameter is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var result = await service.EditNewsCommentAsync(articleId, commentId, userId, updatedText, ct);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Failed to edit comment (by user ID).", ex.Message);
            }
        })
            .ExcludeFromDescription()
            .WithName("EditNewsCommentByUserId")
            .WithTags("News")
            .WithSummary("Edit comment with explicit user ID")
            .WithDescription("Used when the client provides the user ID directly in the query.")
            .Produces<NewsCommentApiResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/landing", async Task<Results<
            Ok<GenericNewsPageResponse>,
            NotFound<ProblemDetails>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>> (
        [FromQuery] int categoryId,
        [FromQuery] int subCategoryId,
        IV2NewsService service,
        CancellationToken ct) =>
        {
            try
            {
                var page = await service.GetNewsLandingPageAsync(categoryId, subCategoryId, ct);
                return TypedResults.Ok(page);
            }
            catch (KeyNotFoundException knf)
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = knf.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (ArgumentException arg)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = arg.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            catch (Exception ex)
            {
                return TypedResults.Problem(new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        })
        .WithName("GetNewsLandingPage")
        .WithTags("News")
        .WithSummary("Get the 6-section news landing page for a category/subcategory");

        return group;
    }
}
