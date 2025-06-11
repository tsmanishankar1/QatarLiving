using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Security.Claims;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints
{
    public static class EventEndpoints
    {
        public static RouteGroupBuilder MapCreateEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            ContentEventDto dto,
            IEventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!Guid.TryParse(userId, out var userGuid))
                        return TypedResults.Forbid();

                    var userName = httpContext.User.FindFirst("preferred_username")?.Value
                                ?? httpContext.User.FindFirst("name")?.Value
                                ?? httpContext.User.Identity?.Name;

                    if (string.IsNullOrWhiteSpace(userName))
                        return TypedResults.Forbid();

                    dto.user_name = userName;

                    var result = await service.CreateEvent(dto, cancellationToken);
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
            .WithName("CreateEvent")
            .WithTags("Event")
            .WithSummary("Create Event")
            .WithDescription("Creates a new event and sets the user ID from the token.")
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
            ContentEventDto dto,
            IEventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (dto.user_name == string.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    var result = await service.CreateEvent(dto, cancellationToken);
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
            .WithName("CreateEvent")
            .WithTags("Event")
            .WithSummary("Create Event")
            .WithDescription("Creates a new event and sets the user ID from the token.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

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
