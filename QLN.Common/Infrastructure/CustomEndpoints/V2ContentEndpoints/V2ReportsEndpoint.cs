using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Text.Json;


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
     CancellationToken cancellationToken
 ) =>
            {
                try
                {
                    var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("name").GetString();
                    dto.AuthorName = uid;

                    if (string.IsNullOrEmpty(userClaim))
                    {
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
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = $"Failed to parse user claim or extract UID: {ex.Message}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateArticleComment(uid, dto, cancellationToken);
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
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(dto.AuthorName))


                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "ReporterName cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateArticleComment(dto.AuthorName, dto, cancellationToken);
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
                 CancellationToken cancellationToken
             ) =>
            {
                try
                {
                    var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("name").GetString();
                    dto.ReporterName = uid;

                    if (string.IsNullOrEmpty(userClaim))
                    {
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
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = $"Failed to parse user claim or extract UID: {ex.Message}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateReport(uid, dto, cancellationToken);
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
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(dto.ReporterName))


                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "ReporterName cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateReport(dto.ReporterName, dto, cancellationToken);
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
     CancellationToken cancellationToken
 ) =>
            {
                try
                {
                    var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("name").GetString();
                    dto.ReporterName = uid;

                    if (string.IsNullOrEmpty(userClaim))
                    {
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
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = $"Failed to parse user claim or extract UID: {ex.Message}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityCommentReport(uid, dto, cancellationToken);
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
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(dto.ReporterName))


                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "ReporterName cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityCommentReport(dto.ReporterName, dto, cancellationToken);
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
     CancellationToken cancellationToken
 ) =>
            {
                try
                {
                    var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("name").GetString();
                    dto.ReporterName = uid;

                    if (string.IsNullOrEmpty(userClaim))
                    {
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
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = $"Failed to parse user claim or extract UID: {ex.Message}",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityReport(uid, dto, cancellationToken);
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
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(dto.ReporterName))


                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "ReporterName cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityReport(dto.ReporterName, dto, cancellationToken);
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
            group.MapGet("/getAll", static async Task<Results<Ok<List<V2ContentReportArticleResponseDto>>, ProblemHttpResult>>
            (
                IV2ReportsService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var reports = await service.GetAllReports(cancellationToken);
                    return TypedResults.Ok(reports);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllReports")
            .WithTags("Reports")
            .WithSummary("Get All Reports")
            .WithDescription("Retrieves all reports.")
            .Produces<List<V2ContentReportArticleResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}