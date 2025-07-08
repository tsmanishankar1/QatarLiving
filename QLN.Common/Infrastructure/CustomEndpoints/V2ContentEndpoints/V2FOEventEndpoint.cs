using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2FOEventEndpoint
    {
        public static RouteGroupBuilder MapGetAllFOFeaturedEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getallfofeaturedevents", static async Task<Results<Ok<List<V2Events>>, NotFound<ProblemDetails>, ProblemHttpResult>>
            (
                bool isFeatured,
                IV2FOEventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var events = await service.GetAllFOIsFeaturedEvents(isFeatured, cancellationToken);
                    if (events == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Featured Events Found",
                            Detail = "There are no featured events available at this time.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(events);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllFOFeaturedEvents")
            .WithTags("FOEvent")
            .WithSummary("Get All Events")
            .WithDescription("Retrieves all events.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetFOEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getfobyid/{id:guid}", async Task<Results<Ok<V2Events>, NotFound<ProblemDetails>, ProblemHttpResult>>
                (
                    Guid id,
                    IV2FOEventService service,
                    CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetFOEventById(id, cancellationToken);
                    if (result == null || result.IsActive == false)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Event Not Found",
                            Detail = $"Active event with ID '{id}' not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .WithName("GetFOEventById")
                .WithTags("FOEvent")
                .WithSummary("Get Event By ID")
                .WithDescription("Retrieves a single event by its GUID identifier.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetFOPaginatedEvents(this RouteGroupBuilder group)
        {
            group.MapPost("/getfopaginatedevents", async Task<Results<Ok<PagedResponse<V2Events>>,
                BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ProblemHttpResult>>
            (
                [FromBody] GetPagedEventsRequest request,
                IV2FOEventService service,
                CancellationToken cancellationToken = default
            ) =>
            {
                try
                {
                    var result = await service.GetFOPagedEvents(request, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Server Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
            .WithName("GetFOPaginatedEvents")
            .WithTags("FOEvent")
            .WithSummary("Paginated Events List")
            .WithDescription("Fetches events with support for filtering, sorting, and pagination.")
            .Produces<PagedResponse<V2Events>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
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
            .WithTags("FOEvent")
            .WithSummary("Get Event by Slug")
            .WithDescription("Retrieves event details using SEO-friendly slug.")
            .Produces<V2Events>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
