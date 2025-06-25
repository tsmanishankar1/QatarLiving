using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using Microsoft.AspNetCore.Builder;
using QLN.Common.Infrastructure.Utilities;

public static class V2NewsEndpoints
{
    public static RouteGroupBuilder MapCreateNewsEndpoints(this RouteGroupBuilder group)
    {
        // CREATE - Authenticated
        group.MapPost("/createNews", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            V2ContentNewsDto dto,
            IV2NewsService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
                Guid? userId = httpContext.User.GetId();
                string userName = httpContext.User.GetName();

                if (!userId.HasValue || string.IsNullOrWhiteSpace(userName))
                    return TypedResults.Forbid();

                dto.UserId = userId.Value;
                dto.authorName = userName;

                var result = await service.CreateNews(dto, cancellationToken);
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
        .WithName("CreateNews")
        .WithTags("News")
        .WithSummary("Create News")
        .WithDescription("Creates a new news entry and sets the user ID from the token.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        //.RequireAuthorization();

        // CREATE - By direct ID (external service)
        group.MapPost("/createNewBy-Id", async Task<Results<
            Ok<string>,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            V2ContentNewsDto dto,
            IV2NewsService service,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
                if (dto.UserId == Guid.Empty || string.IsNullOrWhiteSpace(dto.authorName))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "UserId and UserName must be provided in the payload.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var result = await service.CreateNews(dto, cancellationToken);
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
        .WithName("CreateNewsByUserId")
        .WithTags("News")
        .WithSummary("Create News By User")
        .WithDescription("Creates a news item using UserId and UserName passed from the frontend.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        //Get All
        group.MapGet("/getAllnews", async Task<Results<
            Ok<List<V2ContentNewsDto>>,
            ProblemHttpResult>>
        (
            IV2NewsService service,
             CancellationToken cancellationToken
        ) =>
        {
            try
            {
                var events = await service.GetAllNews(cancellationToken);
                return TypedResults.Ok(events);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message);
            }
        })
        .WithName("GetAllNews")
        .WithTags("News")
        .WithSummary("Get All News")
        .WithDescription("Returns all news items for the authenticated user.")
        .Produces<List<V2ContentNewsDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // GET by ID
        group.MapGet("getById/{id:guid}", async Task<Results<
            Ok<V2ContentNewsDto>,
            NotFound,
            ProblemHttpResult>>
        (
            Guid id,
            IV2NewsService service,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
                var result = await service.GetNewsById(id, cancellationToken);
                return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();

            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message);
            }
        })
        .WithName("GetNewsById")
        .WithTags("News")
        .WithSummary("Get News")
        .WithDescription("Gets a news entry by its ID.")
        .Produces<V2ContentNewsDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // UPDATE
        group.MapPut("/update", async Task<Results<
       Ok<string>,
       ForbidHttpResult,
       BadRequest<ProblemDetails>,
       NotFound<ProblemDetails>,
       ProblemHttpResult>>
   (
       V2ContentNewsDto dto,
       IV2NewsService service,
       HttpContext httpContext,
       CancellationToken cancellationToken
   ) =>
        {
            try
            {
                Guid? userId = httpContext.User.GetId();
                string userName = httpContext.User.GetName();

                if (!userId.HasValue || string.IsNullOrWhiteSpace(userName))
                    return TypedResults.Forbid();

                dto.UserId = userId.Value;
                dto.authorName = userName;

                if (dto.Id == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "News ID must be provided.",
                        Status = 400
                    });

                dto.UserId = userId.Value;
                dto.authorName = userName;

                var result = await service.UpdateNews(dto, cancellationToken);
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
   .WithName("UpdateNews")
   .WithTags("News")
   .WithSummary("Update News (Authenticated)")
   .WithDescription("Updates a news entry and assigns the user info from access token.")
   .Produces<string>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(404)
   .Produces<ProblemDetails>(500);
   //.RequireAuthorization();

        group.MapPut("/updateNewsByUserId", async Task<Results<
    Ok<string>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    V2ContentNewsDto dto,
    IV2NewsService service,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                if (dto.UserId == Guid.Empty || string.IsNullOrWhiteSpace(dto.authorName))
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "UserId and UserName must be provided in the payload.",
                        Status = 400
                    });

                if (dto.Id == Guid.Empty)
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "News ID must be provided.",
                        Status = 400
                    });

                var result = await service.UpdateNews(dto, cancellationToken);
                return TypedResults.Ok(result);
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
.WithName("UpdateNewsByUserId")
.WithTags("News")
.WithSummary("Update News (Public)")
.WithDescription("Updates a news entry using values provided in the request payload.")
.Produces<string>(200)
.Produces<ProblemDetails>(400)
 .ExcludeFromDescription()
.Produces<ProblemDetails>(500);

        // DELETE
        group.MapDelete("/delete/{id:guid}", async Task<Results<
            NoContent,
            NotFound,
            ProblemHttpResult>>
        (
            Guid id,
            IV2NewsService service,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
                var deleted = await service.DeleteNews(id, cancellationToken);
                return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message);
            }
        })
        .WithName("DeleteNews")
        .WithTags("News")
        .WithSummary("Delete News")
        .WithDescription("Deletes a news entry by its ID.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        //Category

        // CREATE - Authenticated
        group.MapPost("/createNewsCategory", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
        (
            NewsCategoryDto dto,
            IV2NewsService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
            //    Guid? userId = httpContext.User.GetId();
            //    string userName = httpContext.User.GetName();

            //    if (!userId.HasValue || string.IsNullOrWhiteSpace(userName))
            //        return TypedResults.Forbid();

            //    //dto.UserId = userId.Value;
            //    //dto.authorName = userName;

            var result = await service.CreateNewsCategoryAsync(dto, cancellationToken);
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
        .WithName("createNewsCategory")
        .WithTags("News")
        .WithSummary("Create News")
        .WithDescription("Creates a new news createNewsCategory")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        //.RequireAuthorization();

        group.MapGet("/getAllnewsCategory", async Task<Results<
                 Ok<List<NewsCategoryDto>>,
                 ProblemHttpResult>>
             (
                 IV2NewsService service,
                  CancellationToken cancellationToken
             ) =>
        {
            try
            {
                var events = await service.GetAllNewsCategoriesAsync(cancellationToken);
                return TypedResults.Ok(events);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message);
            }
        })
             .WithName("getAllnewsCategory")
             .WithTags("News")
             .WithSummary("Get All News Category")
             .WithDescription("Returns all news Category items")
             .Produces<List<NewsCategoryDto>>(StatusCodes.Status200OK)
             .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapGet("/getWriterTags", async Task<Results<
         Ok<Dictionary<string, string>>,
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

        group.MapPost("/createNewsArticle", async Task<Results<
            Ok<CreateNewsArticleResponseDto>,
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
                Guid? userId = httpContext.User.GetId();
                string userName = httpContext.User.GetName();

                if (!userId.HasValue || string.IsNullOrWhiteSpace(userName))
                    return TypedResults.Forbid();

                dto.CreatedBy = userId.Value;
                dto.UpdatedBy = userId.Value;
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;

                var result = await service.CreateNewsArticleAsync(userId.Value, dto, cancellationToken);
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
         .WithDescription("Creates a news article using authenticated user ID and name from token.")
         .Produces<CreateNewsArticleResponseDto>(StatusCodes.Status200OK)
         .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
         .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
         .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


        group.MapPost("/createNewsArticleById", async Task<Results<
            Ok<CreateNewsArticleResponseDto>,
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
                if (dto.CreatedBy == Guid.Empty || dto.UpdatedBy == Guid.Empty)
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
        .Produces<CreateNewsArticleResponseDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/getAll", async Task<Results<
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

        group.MapPost("/createNewsArticleCategory", async Task<Results<Ok<string>, ForbidHttpResult, BadRequest<ProblemDetails>, ProblemHttpResult>> (
    V2NewsCategory dto,
    IV2NewsService service,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
        {
            try
            {

                var result = await service.CreateNewsArticleCategoryAsync(dto, cancellationToken);
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
        .WithName("createNewsArticleCategory")
        .WithTags("News")
        .WithSummary("Create News")
        .WithDescription("Creates a new news createNewsCategory")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/getAllNewsArticleCategories", async Task<Results<Ok<List<V2NewsCategory>>, ProblemHttpResult>> (
    IV2NewsService service,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
        {
            try
            {
                var result = await service.GetAllNewsArticleCategoriesAsync(cancellationToken);
                return TypedResults.Ok(result);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem("Internal Server Error", ex.Message);
            }
        })
.WithName("getAllNewsArticleCategories")
.WithTags("News")
.WithSummary("Get All News Categories")
.WithDescription("Retrieves all news article categories.")
.Produces<List<V2NewsCategory>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return group;
    }
}
