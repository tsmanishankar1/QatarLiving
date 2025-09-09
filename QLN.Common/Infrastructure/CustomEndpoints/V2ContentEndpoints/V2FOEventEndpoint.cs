using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using Microsoft.AspNetCore.Builder;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2FOEventEndpoint
    {
        public static RouteGroupBuilder MapGetEventBySlugEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/slug/{slug}", async Task<Results<Ok<V2Events>, NotFound<ProblemDetails>, ProblemHttpResult>> (
                string slug,
                IV2FOEventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetEventBySlug(slug, cancellationToken);
                    return result is not null
                        ? TypedResults.Ok(result)
                        : TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No event found with slug '{slug}'"
                        });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetEventsBySlug")
            .WithTags("Event")
            .WithSummary("Get Event by Slug")
            .WithDescription("Retrieves event details using SEO-friendly slug.")
            .Produces<V2Events>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
    }
}
