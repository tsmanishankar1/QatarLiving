using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Text.Json;
using static QLN.Common.DTO_s.V2ReportCommunityPost;


namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2ReportsEndpoints
    {
        public static RouteGroupBuilder MapCreateNewsCommentEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/createarticlecomments", async Task<Results<
     Ok<string>,
     ForbidHttpResult,
     BadRequest<ProblemDetails>,
     ProblemHttpResult>>
 (
     V2NewsCommunitycommentsDto dto,
     IV2ReportsService service,
     HttpContext context,
     ILogger<IV2ReportsService> logger,
     CancellationToken cancellationToken
 ) =>
            {
                try
                {
                    logger.LogInformation("CreateArticleComments called");

                    var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("name").GetString();
                    dto.AuthorName = uid;

                    if (string.IsNullOrEmpty(userClaim))
                    {
                        logger.LogWarning("User claim not found in token");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = "User claim not found in token.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    try
                    {
                        if (string.IsNullOrEmpty(uid))
                        {
                            logger.LogWarning("UID is missing in user claim");
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Authentication Error",
                                Detail = "UID is missing in user claim.",
                                Status = StatusCodes.Status400BadRequest
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse user claim or extract UID");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = $"Failed to parse user claim or extract UID: {ex.Message}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateArticleComment(uid, dto, cancellationToken);
                    logger.LogInformation("Article comment created successfully for user: {UserId}", uid);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "Invalid data provided for creating article comment");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in CreateArticleComments endpoint");
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
        .WithName("CreateArticleComments")
        .WithTags("Reports")
        .WithSummary("Create Report")
        .WithDescription("Creates a new report and sets the user ID from the token.")
        .Produces<string>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/createarticlecommentByUserId", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            V2NewsCommunitycommentsDto dto,
            IV2ReportsService service,
            HttpContext httpContext,
            ILogger<IV2ReportsService> logger,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    logger.LogInformation("CreateArticleCommentByUserId called for user: {AuthorName}", dto.AuthorName);

                    if (string.IsNullOrEmpty(dto.AuthorName))
                    {
                        logger.LogWarning("AuthorName is null or empty");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "ReporterName cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateArticleComment(dto.AuthorName, dto, cancellationToken);
                    logger.LogInformation("Article comment created successfully by user ID: {AuthorName}", dto.AuthorName);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "Invalid data provided for creating article comment by user ID");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in CreateArticleCommentByUserId endpoint");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("CreateNewsArticleCommentsByUserId")
            .WithTags("News")
            .WithSummary("Create Article Comments")
            .WithDescription("Creates a new report with provided user ID.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapCreateReportEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async Task<Results<
                 Ok<string>,
                 ForbidHttpResult,
                 BadRequest<ProblemDetails>,
                 ProblemHttpResult>>
             (
                 V2ContentReportArticleDto dto,
                 IV2ReportsService service,
                 HttpContext context,
                 ILogger<IV2ReportsService> logger,
                 CancellationToken cancellationToken
             ) =>
            {
                try
                {
                    logger.LogInformation("CreateReport called");

                    var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("name").GetString();
                    dto.ReporterName = uid;

                    if (string.IsNullOrEmpty(userClaim))
                    {
                        logger.LogWarning("User claim not found in token");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = "User claim not found in token.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    try
                    {
                        if (string.IsNullOrEmpty(uid))
                        {
                            logger.LogWarning("UID is missing in user claim");
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Authentication Error",
                                Detail = "UID is missing in user claim.",
                                Status = StatusCodes.Status400BadRequest
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse user claim or extract UID");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = $"Failed to parse user claim or extract UID: {ex.Message}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateReport(uid, dto, cancellationToken);
                    logger.LogInformation("Report created successfully for user: {UserId}", uid);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "Invalid data provided for creating report");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in CreateReport endpoint");
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
         .WithName("CreateArticleReport")
         .WithTags("Reports")
         .WithSummary("Create Report")
         .WithDescription("Creates a new report and sets the user ID from the token.")
         .Produces<string>(StatusCodes.Status200OK)
         .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
         .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
         .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/createByUserId", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            V2ContentReportArticleDto dto,
            IV2ReportsService service,
            HttpContext httpContext,
            ILogger<IV2ReportsService> logger,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    logger.LogInformation("CreateReportByUserId called for user: {ReporterName}", dto.ReporterName);

                    if (string.IsNullOrEmpty(dto.ReporterName))
                    {
                        logger.LogWarning("ReporterName is null or empty");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "ReporterName cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateReport(dto.ReporterName, dto, cancellationToken);
                    logger.LogInformation("Report created successfully by user ID: {ReporterName}", dto.ReporterName);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "Invalid data provided for creating report by user ID");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in CreateReportByUserId endpoint");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("CreateArticleReportByUserId")
            .WithTags("Reports")
            .WithSummary("Create Report")
            .WithDescription("Creates a new report with provided user ID.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapCreateCommunityCommentsEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/createcommunitycomments", async Task<Results<
     Ok<string>,
     ForbidHttpResult,
     BadRequest<ProblemDetails>,
     ProblemHttpResult>>
 (
     V2ReportsCommunitycommentsDto dto,
     IV2ReportsService service,
     HttpContext context,
     ILogger<IV2ReportsService> logger,
     CancellationToken cancellationToken
 ) =>
            {
                try
                {
                    logger.LogInformation("CreateCommunityComments called");

                    var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("name").GetString();
                    dto.ReporterName = uid;

                    if (string.IsNullOrEmpty(userClaim))
                    {
                        logger.LogWarning("User claim not found in token");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = "User claim not found in token.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    try
                    {
                        if (string.IsNullOrEmpty(uid))
                        {
                            logger.LogWarning("UID is missing in user claim");
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Authentication Error",
                                Detail = "UID is missing in user claim.",
                                Status = StatusCodes.Status400BadRequest
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse user claim or extract UID");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = $"Failed to parse user claim or extract UID: {ex.Message}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityCommentReport(uid, dto, cancellationToken);
                    logger.LogInformation("Community comment report created successfully for user: {UserId}", uid);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "Invalid data provided for creating community comment report");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in CreateCommunityComments endpoint");
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
.WithName("CreateCommunityCommentsReport")
.WithTags("Reports")
.WithSummary("Create Report")
.WithDescription("Creates a new report and sets the user ID from the token.")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/createcommunitycommentsByUserId", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            V2ReportsCommunitycommentsDto dto,
            IV2ReportsService service,
            HttpContext httpContext,
            ILogger<IV2ReportsService> logger,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    logger.LogInformation("CreateCommunityCommentsByUserId called for user: {ReporterName}", dto.ReporterName);

                    if (string.IsNullOrEmpty(dto.ReporterName))
                    {
                        logger.LogWarning("ReporterName is null or empty");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "ReporterName cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityCommentReport(dto.ReporterName, dto, cancellationToken);
                    logger.LogInformation("Community comment report created successfully by user ID: {ReporterName}", dto.ReporterName);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "Invalid data provided for creating community comment report by user ID");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in CreateCommunityCommentsByUserId endpoint");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("CreateCommunityCommentsReportByUserId")
            .WithTags("Reports")
            .WithSummary("CreateCommunityCommentsReport")
            .WithDescription("Creates a new report with provided user ID.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapCreateCommunityPostReportEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/createcommunitypost", async Task<Results<
     Ok<string>,
     ForbidHttpResult,
     BadRequest<ProblemDetails>,
     ProblemHttpResult>>
 (
     V2ReportCommunityPostDto dto,
     IV2ReportsService service,
     HttpContext context,
     ILogger<IV2ReportsService> logger,
     CancellationToken cancellationToken
 ) =>
            {
                try
                {
                    logger.LogInformation("CreateCommunityPost called");

                    var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("name").GetString();
                    dto.ReporterName = uid;

                    if (string.IsNullOrEmpty(userClaim))
                    {
                        logger.LogWarning("User claim not found in token");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = "User claim not found in token.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    try
                    {
                        if (string.IsNullOrEmpty(uid))
                        {
                            logger.LogWarning("UID is missing in user claim");
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Authentication Error",
                                Detail = "UID is missing in user claim.",
                                Status = StatusCodes.Status400BadRequest
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse user claim or extract UID");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = $"Failed to parse user claim or extract UID: {ex.Message}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityReport(uid, dto, cancellationToken);
                    logger.LogInformation("Community post report created successfully for user: {UserId}", uid);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "Invalid data provided for creating community post report");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in CreateCommunityPost endpoint");
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("CreateCommunityPostReport")
            .WithTags("Reports")
            .WithSummary("Create Report")
            .WithDescription("Creates a new report and sets the user ID from the token.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/createcommunitypostByUserId", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            V2ReportCommunityPostDto dto,
            IV2ReportsService service,
            HttpContext httpContext,
            ILogger<IV2ReportsService> logger,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    logger.LogInformation("CreateCommunityPostByUserId called for user: {ReporterName}", dto.ReporterName);

                    if (string.IsNullOrEmpty(dto.ReporterName))
                    {
                        logger.LogWarning("ReporterName is null or empty");
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "ReporterName cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityReport(dto.ReporterName, dto, cancellationToken);
                    logger.LogInformation("Community post report created successfully by user ID: {ReporterName}", dto.ReporterName);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "Invalid data provided for creating community post report by user ID");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in CreateCommunityPostByUserId endpoint");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("CreateCommunityPostReportByUserId")
            .WithTags("Reports")
            .WithSummary("CreateCommunityPostReport")
            .WithDescription("Creates a new report with provided user ID.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapGetAllReportsEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getAll", async Task<Results<Ok<List<V2ContentReportArticleResponseDto>>, ProblemHttpResult>> (
                IV2ReportsService service,
                ILogger<IV2ReportsService> logger,
                string sortOrder = "desc",
                int pageNumber = 1,
                int pageSize = 12,
                string? searchTerm = null,
                CancellationToken cancellationToken = default
            ) =>
            {
                try
                {
                    logger.LogInformation("GetAllReports called with sortOrder: {SortOrder}, pageNumber: {PageNumber}, pageSize: {PageSize}, searchTerm: {SearchTerm}",
                        sortOrder, pageNumber, pageSize, searchTerm);

                    var reports = await service.GetAllReports(sortOrder, pageNumber, pageSize, searchTerm, cancellationToken);

                    logger.LogInformation("Retrieved {Count} reports", reports.Count);

                    return TypedResults.Ok(reports);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in GetAllReports endpoint");
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetAllReports")
            .WithTags("Reports")
            .WithSummary("Get All Reports with Pagination and Search")
            .WithDescription("Retrieves a paginated list of reports sorted by report date. Supports 'sortOrder', 'pageNumber', 'pageSize', and 'searchTerm'.")
            .Produces<List<V2ContentReportArticleResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapUpdateArticleCommentStatusEndpoints(this RouteGroupBuilder group)
        {
            group.MapPut("/updatearticlecommentstatus", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                V2UpdateReportStatusDto dto,
                IV2ReportsService contentService,
                ILogger<IV2ReportsService> logger,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    logger.LogInformation("➡️ Received update request with IsKeep: {IsKeep}, IsDelete: {IsDelete}", dto.IsKeep, dto.IsDelete);

                    var result = await contentService.UpdateReportStatus(dto, cancellationToken);

                    logger.LogInformation("✅ Article comment/report status update completed successfully.");
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "❌ Invalid data error while updating article comment/report status: {Message}", ex.Message);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Unexpected error while updating article comment/report status: {Message}", ex.Message);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("UpdateArticleCommentStatus")
            .WithTags("Reports")
            .WithSummary("Update Article or Comment Status")
            .WithDescription("Marks reports or their associated comments as inactive based on IsKeep/IsDelete flags. " +
                             "This action uses report and comment indexes for lookup instead of direct ReportId.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }


        public static RouteGroupBuilder MapGetReportCommunityPost(this RouteGroupBuilder group)
        {
            group.MapGet("/getpostwithreports", async Task<Results<
            Ok<CommunityPostWithReports>,
            NotFound,
            ProblemHttpResult
            >> (
            Guid postId,
            IV2ReportsService service,
            CancellationToken ct
            ) =>
            {
                try
                {
                    var result = await service.GetCommunityPostWithReport(postId, ct);
                    if (result is null)
                        return TypedResults.NotFound();

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetPostWithReports")
            .WithTags("Reports")
            .WithSummary("Get community post with associated reports")
            .WithDescription("Returns basic details of a community post along with its reports.")
            .Produces<CommunityPostWithReports>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetAllCommunityPostReports(this RouteGroupBuilder group)
        {
            group.MapGet("/getallcommunitypostwithreports", async Task<Results<
                Ok<List<CommunityPostWithReports>>,
                ProblemHttpResult
                >> (
                IV2ReportsService service,
                CancellationToken ct
                ) =>
            {
                try
                {
                    var result = await service.GetAllCommunityPostsWithReports(ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .WithName("GetAllCommunityPostReports")
                .WithTags("Reports")
                .WithSummary("Get all community posts with associated reports")
                .WithDescription("Returns a list of community posts along with their reports.")
                .Produces<List<CommunityPostWithReports>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetAllCommunityPostsWithPagination(this RouteGroupBuilder group)
        {
            group.MapGet("/getallcommunitypostswithpagination", async Task<Results<
                Ok<V2ReportCommunityPost.PaginatedCommunityPostResponse>,
                ProblemHttpResult
                >> (
                IV2ReportsService service,
                int? pageNumber,
                int? perPage,
                string? searchTitle = null,
                string? sortBy = null,
                CancellationToken ct = default
                ) =>
            {
                try
                {
                    var result = await service.GetAllCommunityPostsWithPagination(pageNumber, perPage, searchTitle, sortBy, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllCommunityPostsWithPagination")
            .WithTags("Reports")
            .WithSummary("Get all community posts with pagination")
            .WithDescription("Returns a paginated list of community posts along with their reports.")
            .Produces<V2ReportCommunityPost.PaginatedCommunityPostResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}