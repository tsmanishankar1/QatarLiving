using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.SearchService.IndexModels;
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
                    [FromServices] ISearchService svc)
                => TypedResults.Ok(await svc.SearchAsync(vertical, req)))
                .WithOpenApi();

            group.MapPost("/upload", async (
                    [FromRoute] string vertical,
                    [FromBody] SearchDocument doc,
                    [FromServices] ISearchService svc)
                => TypedResults.Ok(new
                {
                    Message = await svc.UploadAsync(vertical, doc)
                }))
                .WithOpenApi();

            return group;
        }
    }
}
