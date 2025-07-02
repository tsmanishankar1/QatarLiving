using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Text.Json;

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
            V2Events dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    dto.CreatedBy = uid;
                    var result = await service.CreateEvent(uid, dto, cancellationToken);
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
            V2Events dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (dto.CreatedBy == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "CreatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.CreateEvent(dto.CreatedBy, dto, cancellationToken);
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
            return group;
        }
        public static RouteGroupBuilder MapGetAllEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getAll", static async Task<Results<Ok<List<V2Events>>, ProblemHttpResult>>
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
            return group;
        }
        public static RouteGroupBuilder MapGetEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getById/{id:guid}", async Task<Results<Ok<V2Events>, NotFound<ProblemDetails>, ProblemHttpResult>>
                (
                    Guid id,
                    IV2EventService service,
                    CancellationToken cancellationToken
                ) =>
                {
                    try
                    {
                        var result = await service.GetEventById(id, cancellationToken);
                        if (result == null || result.IsActive == false)
                            throw new KeyNotFoundException($"Active event with ID '{id}' not found.");
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
                .WithName("GetEventById")
                .WithTags("Event")
                .WithSummary("Get Event By ID")
                .WithDescription("Retrieves a single event by its GUID identifier.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapUpdateEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapPut("/update", async Task<Results<Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>>
            (
            V2Events dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    dto.UpdatedBy = uid;
                    var result = await service.UpdateEvent(uid, dto, cancellationToken);
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
            V2Events dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (dto.UpdatedBy == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "CreatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.UpdateEvent(dto.UpdatedBy, dto, cancellationToken);
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
            return group;
        }
        public static RouteGroupBuilder MapDeleteEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapDelete("/delete/{id:guid}", async Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>>
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
        public static RouteGroupBuilder MapCreateCategories(this RouteGroupBuilder group)
        {
            group.MapPost("/createCategory", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>>
            (
                EventsCategory dto,
                IV2EventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.CreateCategory(dto, cancellationToken);
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
            .WithName("CreateEventCategory")
            .WithTags("Event")
            .WithSummary("Create Event Category")
            .WithDescription("Creates a new event category.")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapEventCategories(this RouteGroupBuilder group)
        {
            group.MapGet("/getAllCategories", static async Task<Results<Ok<List<EventsCategory>>, ProblemHttpResult>> (
                IV2EventService service,
                CancellationToken cancellationToken = default
            ) =>
            {
                try
                {
                    var eventCategories = await service.GetAllCategories(cancellationToken);
                    return TypedResults.Ok(eventCategories);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllEventCategories")
            .WithTags("Event")
            .WithSummary("Get All Event Categories")
            .WithDescription("Retrieves all event categories.")
            .Produces<List<EventsCategory>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetEventCategory(this RouteGroupBuilder group)
        {
            group.MapGet("/getCategoryById/{id:int}", async Task<Results<Ok<EventsCategory>, NotFound<ProblemDetails>, ProblemHttpResult>>
                (
                int id,
                IV2EventService service,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetEventCategoryById(id, cancellationToken);
                    if (result == null)
                        throw new KeyNotFoundException($"Active event with ID '{id}' not found.");
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
                .WithName("GetEventCategoryById")
                .WithTags("Event")
                .WithSummary("Get Event Category By ID")
                .WithDescription("Retrieves a single event.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetEventCategories(this RouteGroupBuilder group)
        {
            group.MapGet("/getPagination", async Task<Results<Ok<PagedResponse<V2Events>>, NotFound<ProblemDetails>, ProblemHttpResult>>
            (
                IV2EventService service,
                CancellationToken cancellationToken,
                [FromQuery] int? page,
                [FromQuery] int? perPage,
                [FromQuery] string? search = null,
                [FromQuery] int? sortBy = 1,
                [FromQuery] string? sortOrder = "asc"
            ) =>
            {
                try
                {
                    var result = await service.GetPagedEventCategories(page, perPage, search, sortBy, sortOrder, cancellationToken);

                    if (result == null || !result.Items.Any())
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "No event categories found.",
                            Status = StatusCodes.Status404NotFound
                        });

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("Get Pagination")
            .WithTags("Event")
            .WithSummary("Get Pagination")
            .WithDescription("Retrieves paginated event categories with optional filters and sorting.")
            .Produces<PagedResponse<EventsCategory>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetAllEventSlot(this RouteGroupBuilder group)
        {
            group.MapGet("/slots", async Task<Results<Ok<List<V2Slot>>, ProblemHttpResult>> (
            IV2EventService service,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetAllEventSlot(cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem($"Unexpected error: {ex.Message}");
                }
            })
            .WithName("GetAllEventSlots")
            .WithTags("Event")
            .WithSummary("Get All Event Slots")
            .WithDescription("Returns a list of all slot enum values and names.")
            .Produces<List<V2Slot>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapExpiredEvents(this RouteGroupBuilder group)
        {
            group.MapGet("/getExpiredEvents", async Task<Results<Ok<IEnumerable<V2Events>>, ProblemHttpResult>>
            (
                IV2EventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var expired = await service.GetExpiredEvents(cancellationToken);
                    return TypedResults.Ok(expired);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
            .WithName("GetExpiredEvents")
            .WithTags("Event")
            .WithSummary("Get Expired Events")
            .WithDescription("Returns events where EndDate is before today.")
            .Produces<IEnumerable<V2Events>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
    }
}
