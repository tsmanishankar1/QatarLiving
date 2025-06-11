using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints
{
    public static class EventEndpoints
    {
        public static RouteGroupBuilder MapCreateEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>>
            (
                ContentEventDto dto,
                IEventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.CreateEvent(dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("CreateEvent")
            .WithTags("Event")
            .WithSummary("Create Event")
            .WithDescription("Creates a new event and saves it via Dapr state store.");

            group.MapGet("/getAll", async Task<Results<Ok<List<ContentEventDto>>, ProblemHttpResult>>
            (
                IEventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var events = await service.GetAllEvents(cancellationToken);
                    return TypedResults.Ok(events);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllEvents")
            .WithTags("Event")
            .WithSummary("Get All Events")
            .WithDescription("Retrieves all events.");

            group.MapGet("/getById/{id:guid}", async Task<Results<Ok<ContentEventDto>, NotFound, ProblemHttpResult>>
            (
                Guid id,
                IEventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetEventById(id, cancellationToken);
                    if (result == null)
                        return TypedResults.NotFound();

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetEventById")
            .WithTags("Event")
            .WithSummary("Get Event By ID")
            .WithDescription("Retrieves a single event by its GUID identifier.");

            group.MapPut("/update", async Task<Results<Ok<string>, ProblemHttpResult>>
            (
                ContentEventDto dto,
                IEventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.UpdateEvent(dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("UpdateEvent")
            .WithTags("Event")
            .WithSummary("Update Event")
            .WithDescription("Updates a new event and saves it via Dapr state store.");

            group.MapDelete("/delete/{id:guid}", async Task<Results<Ok<bool>, ProblemHttpResult>>
            (
                Guid id,
                IEventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var success = await service.DeleteEvent(id, cancellationToken);
                    return TypedResults.Ok(success);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("DeleteEvent")
            .WithTags("Event")
            .WithSummary("Delete Event")
            .WithDescription("Soft delete a event and saves it via Dapr state store.");

            return group;
        }
    }
}
