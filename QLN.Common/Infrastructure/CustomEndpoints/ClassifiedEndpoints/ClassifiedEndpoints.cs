// QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints.cs
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class ClassifiedEndpoints
    {
        private const string Vertical = Constants.ConstantValues.ClassifiedsVertical;

        public static RouteGroupBuilder MapClassifiedLandingEndpoints(this RouteGroupBuilder group)
        {
            // SEARCH
            group.MapPost("/search", async (
                    [FromBody] CommonSearchRequest req,
                    [FromServices] IClassifiedService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                if (req is null)
                {
                    logger.LogWarning("Search called with null payload");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Search payload is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/search"
                    });
                }

                try
                {
                    var results = await svc.Search(req);
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
                        Instance = $"/api/{Vertical}/search"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Search error");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/search"
                    );
                }
            })
            .WithName("SearchClassified")
            .WithTags("Classified")
            .WithSummary("Search classifieds")
            .Produces<IEnumerable<ClassifiedIndexDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // GET BY ID
            group.MapGet("/{id}", async (
                    [FromRoute] string id,
                    [FromServices] IClassifiedService svc,
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
                        Instance = $"/api/{Vertical}/{id}"
                    });
                }

                try
                {
                    var ad = await svc.GetById(id);
                    if (ad is null)
                        return Results.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No document '{id}' in '{Vertical}'.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = $"/api/{Vertical}/{id}"
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
                        Instance = $"/api/{Vertical}/{id}"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GetById error");
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/{id}"
                    );
                }
            })
            .WithName("GetClassifiedById")
            .WithTags("Classified")
            .WithSummary("Get a classified by its ID")
            .Produces<ClassifiedIndexDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // UPLOAD
            group.MapPost("/upload", async (
                    [FromBody] ClassifiedIndexDto doc,
                    [FromServices] IClassifiedService svc,
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
                        Instance = $"/api/{Vertical}/upload"
                    });
                }

                try
                {
                    var msg = await svc.Upload(doc);
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
                        Instance = $"/api/{Vertical}/upload"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Upload error");
                    return Results.Problem(
                        title: "Upload Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/upload"
                    );
                }
            })
            .WithName("UploadClassified")
            .WithTags("Classified")
            .WithSummary("Upload or create a classified item")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // UPDATE
            group.MapPut("/update", async (
                    [FromBody] ClassifiedIndexDto doc,
                    [FromServices] IClassifiedService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                if (doc is null || string.IsNullOrWhiteSpace(doc.Id))
                {
                    logger.LogWarning("Update called with invalid payload");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document payload with valid Id is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/update"
                    });
                }

                try
                {
                    var msg = await svc.Upload(doc);
                    return Results.Ok(msg);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid update request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/update"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Update error");
                    return Results.Problem(
                        title: "Update Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/update"
                    );
                }
            })
            .WithName("UpdateClassified")
            .WithTags("Classified")
            .WithSummary("Update an existing classified item")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // LANDING
            group.MapGet("/landing", async (
                    [FromServices] IClassifiedService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                try
                {
                    var model = await svc.GetLandingPage();
                    return Results.Ok(model);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid landing request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/landing"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Landing error");
                    return Results.Problem(
                        title: "Landing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/landing"
                    );
                }
            })
            .WithName("GetClassifiedLanding")
            .WithTags("Classified")
            .WithSummary("Get landing-page data for classifieds")
            .Produces<ClassifiedLandingPageResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
