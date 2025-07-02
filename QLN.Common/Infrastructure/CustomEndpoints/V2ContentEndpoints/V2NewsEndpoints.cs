using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using Microsoft.AspNetCore.Builder;
using QLN.Common.Infrastructure.Utilities;
using System.Text.Json;

public static class V2NewsEndpoints
{
    public static RouteGroupBuilder MapCreateNewsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/getWriterTags", async Task<Results<
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

       group.MapGet("/getcategories", async Task<Results<
      Ok<List<V2NewsCategory>>,
      ProblemHttpResult>>
      (
          IV2NewsService service,
          CancellationToken cancellationToken
      ) =>
        {
            try
            {
                var getcategories = await service.GetNewsCategoriesAsync(cancellationToken);
                return TypedResults.Ok(getcategories);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Error retrieving writer tags", ex.Message);
            }
        })
      .WithName("getcategories")
      .WithTags("News")
      .WithSummary("Get all writer tags as key-value JSON")
      .WithDescription("Returns writer tags in a key-value JSON object format")
      .Produces<List<V2NewsCategory>>(StatusCodes.Status200OK)
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

        group.MapGet("/filterbyArticle", async Task<Results<Ok<List<V2NewsArticleDTO>>, ProblemHttpResult>> (
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

        group.MapPost("/createNewsArticle", async Task<Results<
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
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
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

        group.MapGet("/getAllNewsArticle", async Task<Results<
            Ok<List<V2NewsArticleDTO>>,
            ProblemHttpResult>>
        (
            IV2NewsService service,
            CancellationToken cancellationToken
) =>
        {
            try
            {
                var articles = await service.GetAllNewsArticlesAsync(cancellationToken);
                return TypedResults.Ok(articles);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Error retrieving articles", ex.Message);
            }
        })
        .WithName("GetAllNewsArticles")
        .WithTags("News")
        .WithSummary("Get all news articles")
        .WithDescription("Returns all news articles stored in the system.")
        .Produces<List<V2NewsArticleDTO>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/byCategory/{categoryId:int}", async Task<Results<Ok<List<V2NewsArticleDTO>>, ProblemHttpResult>> (
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

        group.MapGet("/byCategory/{categoryId:int}/sub/{subCategoryId:int}", async Task<Results<Ok<List<V2NewsArticleDTO>>, ProblemHttpResult>> (
            int categoryId,
            int subCategoryId,
            IV2NewsService service,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
                var result = await service.GetArticlesBySubCategoryIdAsync(categoryId, subCategoryId, cancellationToken);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message);
            }
        })
        .WithName("GetArticlesBySubCategory")
        .WithTags("News")
        .Produces<List<V2NewsArticleDTO>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


         group.MapPut("/updateNewsArticle", async Task<Results<
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
                        // ✅ Extract user from token
                        var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                        var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                        var uid = userData.GetProperty("uid").GetString();
                        dto.UpdatedBy = uid;

                        // ✅ Basic validation
                        if (dto.Id == Guid.Empty)
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Validation Error",
                                Detail = "News ID must be provided.",
                                Status = 400
                            });

                        dto.UserId = uid;
                        dto.authorName = uid;
                        dto.UpdatedBy = uid;
                        dto.UpdatedAt = DateTime.UtcNow;

                        var result = await service.UpdateNewsArticleAsync(dto, cancellationToken);

                        return TypedResults.Ok(result); // result should be a success message
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

        group.MapDelete("/deleteNews/{id:guid}", async Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>>
            (
                Guid id,
                IV2NewsService service,
                CancellationToken cancellationToken
            ) =>
        {
            try
            {
                var success = await service.DeleteNews(id, cancellationToken);
                if (success == null)
                    throw new KeyNotFoundException($"News with ID '{id}' not found.");
                return TypedResults.Ok(success);
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
                return TypedResults.Problem("Internal Server Error", ex.Message);
            }
        })
            .WithName("DeleteNews")
            .WithTags("News")
            .WithSummary("Delete News")
            .WithDescription("Soft delete a news and saves it via Dapr state store.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        return group;
    }
}
