using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using System.Net.Http;
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

        public static RouteGroupBuilder MapGetReportEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getById/{id:guid}", async Task<Results<Ok<V2ContentReportArticleResponseDto>, NotFound<ProblemDetails>, ProblemHttpResult>>
                (
                    Guid id,
                    IV2ReportsService service,
                    CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetReportById(id, cancellationToken);
                    if (result == null)
                        throw new KeyNotFoundException($"Report with ID '{id}' not found.");
                    return TypedResults.Ok(result);
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
                .WithName("GetReportById")
                .WithTags("Reports")
                .WithSummary("Get Report By ID")
                .WithDescription("Retrieves a single report by its GUID identifier.")
                .Produces<V2ContentReportArticleResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }



        public static RouteGroupBuilder MapUpdateReportEndpoints(this RouteGroupBuilder group)
        {
            group.MapPut("/update", async Task<Results<Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
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
                    // Try to get the user claim
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user");

                    // Check if userClaim exists
                    if (userClaim == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = "User claim not found in token.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    string uid;
                    try
                    {
                        // The user claim value should be a JSON string, parse it
                        var userData = JsonSerializer.Deserialize<JsonElement>(userClaim.Value);
                        uid = userData.GetProperty("uid").GetString();

                        // Check if uid is null or empty
                        if (string.IsNullOrEmpty(uid))
                        {
                            return TypedResults.BadRequest(new ProblemDetails
                            {
                                Title = "Authentication Error",
                                Detail = "User ID not found in token.",
                                Status = StatusCodes.Status400BadRequest
                            });
                        }
                    }
                    catch (JsonException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = "Invalid user claim format in token.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Authentication Error",
                            Detail = "uid property not found in user claim.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.UpdateReport(uid, dto, cancellationToken);
                    return TypedResults.Ok(result);
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
            .WithName("UpdateReport")
            .WithTags("Reports")
            .WithSummary("Update Report")
            .WithDescription("Updates an existing report and saves it via Dapr state store.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/updateByUserId", async Task<Results<Ok<string>,
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

                    var result = await service.UpdateReport(dto.ReporterName, dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("UpdateReportByUserId")
            .WithTags("Reports")
            .ExcludeFromDescription()
            .WithSummary("Update Report")
            .WithDescription("Updates an existing report with provided user ID.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapDeleteReportEndpoints(this RouteGroupBuilder group)
        {
            group.MapDelete("/delete/{id:guid}", async Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>>
            (
                Guid id,
                IV2ReportsService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.DeleteReport(id, cancellationToken);
                    return TypedResults.Ok(result);
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
            .WithName("DeleteReport")
            .WithTags("Reports")
            .WithSummary("Delete Report")
            .WithDescription("Deletes a report from the Dapr state store.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
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