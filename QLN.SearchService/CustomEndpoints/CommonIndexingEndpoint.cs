using System;
using Azure;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.SearchService.IService;
using QLN.SearchService.Models;

namespace QLN.SearchService.CustomEndpoints
{
    public static class CommonIndexingEndpoint
    {
        public static RouteGroupBuilder MapCommonIndexingEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/{vertical}");


            group.MapPost("/search", async (
                    [FromRoute] string vertical,
                    [FromBody] SearchRequest req,
                    [FromServices] ISearchService svc) =>
            {
                if (req == null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Search request payload is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var results = await svc.SearchAsync(vertical, req);
                    return Results.Ok(results);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Vertical",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithOpenApi()
            .WithName("CommonSearch");

            group.MapPost("/upload", async (
                    [FromRoute] string vertical,
                    [FromBody] SearchDocument doc,
                    [FromServices] ISearchService svc) =>
            {
                if (doc == null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document payload is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    // Ensure a non-empty Id key
                    if (!doc.TryGetValue("Id", out var idObj) ||
                        string.IsNullOrWhiteSpace(idObj?.ToString()))
                    {
                        doc["Id"] = Guid.NewGuid().ToString();
                    }

                    var message = await svc.UploadAsync(vertical, doc);
                    return Results.Ok(new { Message = message });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Vertical or Payload",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (RequestFailedException ex)
                {
                    return Results.Problem(
                        title: "Azure Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithOpenApi()
            .WithName("CommonUpload");

            group.MapGet("/{id}", async (
                   [FromRoute] string vertical,
                   [FromRoute] string id,
                   [FromServices] ISearchService svc)
               => {
                   try
                   {
                       var doc = await svc.GetByIdAsync(vertical, id);
                       return doc is null
                           ? Results.NotFound()
                           : Results.Ok(doc);
                   }
                   catch (ArgumentException ex)
                   {
                       return Results.BadRequest(new { ex.Message });
                   }
                   catch
                   {
                       return Results.Problem("Lookup Error");
                   }
               })
               .WithOpenApi();

            return group;
        }
    }
}
