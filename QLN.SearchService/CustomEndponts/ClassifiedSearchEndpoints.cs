using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using QLN.SearchService.IndexModels;
using QLN.SearchService.IService;

namespace QLN.SearchService.CustomEndponts
{
    public static class ClassifiedSearchEndpoints
    {
        public static RouteGroupBuilder MapClassifiedSearchEndpoints(this RouteGroupBuilder group)
        {

            // Search Endpoint
            group.MapPost("/search", async Task<Results<
                Ok<IEnumerable<ClassifiedIndex>>,
                BadRequest<ProblemDetails>,
                ValidationProblem,
                ProblemHttpResult>> (
                [FromBody] SearchRequest request,
                HttpContext context,
                [FromServices] ISearchService searchService
            ) =>
            {
                try
                {
                    var result = await searchService.SearchAsync(request);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred during classified search.",
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("ClassifiedSearch")
            .WithTags("Classifieds")
            .WithSummary("Search classifieds")
            .WithDescription("Performs full-text and filtered classified search using Azure AI Search index.")
            .Produces<IEnumerable<ClassifiedIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Upload Classified Ad Endpoint
            group.MapPost("/upload", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>> (
                [FromBody] ClassifiedIndex ad,
                HttpContext context,
                [FromServices] ISearchService searchService
            ) =>
            {
                try
                {
                    await searchService.UploadAsync(ad);
                    return TypedResults.Ok("Classified ad uploaded successfully.");
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred while uploading the classified ad.",
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("UploadClassifiedAd")
            .WithTags("Classifieds")
            .WithSummary("Upload a classified ad")
            .WithDescription("Uploads a new classified ad (e.g., a mobile phone under electronics) to the Azure Search index.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
