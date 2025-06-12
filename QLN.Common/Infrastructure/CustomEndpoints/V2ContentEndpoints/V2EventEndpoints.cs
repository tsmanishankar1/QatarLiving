using Google.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.Utilities;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints
{
    public static class V2EventEndpoints
    {
        public static RouteGroupBuilder MapCreateEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            V2ContentEventDto dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    Guid? userId = httpContext.User.GetId();
                    string userName = httpContext.User.GetName();

                    if (!userId.HasValue || string.IsNullOrWhiteSpace(userName))
                        return TypedResults.Forbid();
                    dto.User_id = userId ?? Guid.Empty;
                    dto.User_name = userName;

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
            V2ContentEventDto dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (dto.User_id == Guid.Empty || string.IsNullOrWhiteSpace(dto.User_name))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId and UserName must be provided in the payload.",
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
            .WithName("CreateEventByUserId")
            .WithTags("Event")
            .WithSummary("Create Event")
            .WithDescription("Creates a new event and sets the user ID from the token.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getAll", async Task<Results<Ok<List<V2ContentEventDto>>, ProblemHttpResult>>
            (
                IV2EventService service,
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
            .WithDescription("Retrieves all events.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getById/{id:guid}", async Task<Results<Ok<V2ContentEventDto>, NotFound<ProblemDetails>, ProblemHttpResult>>
            (
                Guid id,
                IV2EventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetEventById(id, cancellationToken);
                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Event with ID '{id}' not found.",
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
            .WithName("GetEventById")
            .WithTags("Event")
            .WithSummary("Get Event By ID")
            .WithDescription("Retrieves a single event by its GUID identifier.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/update", async Task<Results<Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>>
            (
            V2ContentEventDto dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    Guid? userId = httpContext.User.GetId();
                    string userName = httpContext.User.GetName();

                    if (!userId.HasValue || string.IsNullOrWhiteSpace(userName))
                        return TypedResults.Forbid();
                    dto.User_id = userId ?? Guid.Empty;
                    dto.User_name = userName;
                    var result = await service.UpdateEvent(dto, cancellationToken);
                    if (result == null)
                        throw new KeyNotFoundException($"Event with ID not found.");
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
            .WithName("UpdateEvent")
            .WithTags("Event")
            .WithSummary("Update Event")
            .WithDescription("Updates a new event and saves it via Dapr state store.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/updateByUserId", async Task<Results<Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            V2ContentEventDto dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (dto.User_id == Guid.Empty || string.IsNullOrWhiteSpace(dto.User_name))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId and UserName must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    var result = await service.UpdateEvent(dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("UpdateEventByUserId")
            .WithTags("Event")
            .ExcludeFromDescription()
            .WithSummary("Update Event")
            .WithDescription("Updates a new event and saves it via Dapr state store.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/delete/{id:guid}", async Task<Results<Ok<string>,NotFound<ProblemDetails>, ProblemHttpResult>>
            (
                Guid id,
                IV2EventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var success = await service.DeleteEvent(id, cancellationToken);
                    if (success == null)
                        throw new KeyNotFoundException($"Event with ID '{id}' not found.");
                    return TypedResults.Ok(success);
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
            .WithName("DeleteEvent")
            .WithTags("Event")
            .WithSummary("Delete Event")
            .WithDescription("Soft delete a event and saves it via Dapr state store.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
