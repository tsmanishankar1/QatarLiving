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
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    if (uid == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
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

            group.MapPost("/createbyuserid", async Task<Results<
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
            group.MapGet("/getall", static async Task<Results<Ok<List<V2Events>>, ProblemHttpResult>>
            (
                IV2EventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var events = await service.GetAllEvents(cancellationToken);
                    return TypedResults.Ok(events ?? new List<V2Events>());
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .AllowAnonymous()
            .WithName("GetAllEvents")
            .WithTags("Event")
            .WithSummary("Get All Events")
            .WithDescription("Retrieves all events.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetAllFeaturedEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getallfeaturedevents", static async Task<Results<Ok<List<V2Events>>, ProblemHttpResult>>
            (
                bool isFeatured,
                IV2EventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var events = await service.GetAllIsFeaturedEvents(isFeatured, cancellationToken);
                    return TypedResults.Ok(events ?? new List<V2Events>());
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .AllowAnonymous()
            .WithName("GetAllFeaturedEvents")
            .WithTags("Event")
            .WithSummary("Get All Events")
            .WithDescription("Retrieves all events.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getbyid/{id:guid}", async Task<Results<Ok<V2Events>, NotFound<ProblemDetails>, ProblemHttpResult>>
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
                .AllowAnonymous()
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
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    if (uid == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
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

            group.MapPut("/updatebyuserid", async Task<Results<Ok<string>,
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
            group.MapPost("/createcategory", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>>
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
            .AllowAnonymous()
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
            group.MapGet("/getallcategories", static async Task<Results<Ok<List<EventsCategory>>, ProblemHttpResult>> (
                IV2EventService service,
                CancellationToken cancellationToken = default
            ) =>
            {
                try
                {
                    var eventCategories = await service.GetAllCategories(cancellationToken);
                    return TypedResults.Ok(eventCategories ?? new List<EventsCategory>());
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .AllowAnonymous()
            .WithName("GetAllEventCategories")
            .WithTags("Event")
            .WithSummary("Get All Event Categories")
            .WithDescription("Retrieves all event categories.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetEventCategory(this RouteGroupBuilder group)
        {
            group.MapGet("/getcategorybyid/{id:int}", async Task<Results<Ok<EventsCategory>, NotFound<ProblemDetails>, ProblemHttpResult>>
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
            .AllowAnonymous()
            .WithName("GetEventCategoryById")
            .WithTags("Event")
            .WithSummary("Get Event Category By ID")
            .WithDescription("Retrieves a single event.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetPaginatedEvents(this RouteGroupBuilder group)
        {
            group.MapPost("/getpaginatedevents", async Task<Results<Ok<PagedResponse<V2Events>>,
                BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ProblemHttpResult>>
            (
                [FromBody] GetPagedEventsRequest request,
                IV2EventService service,
                CancellationToken cancellationToken = default
            ) =>
            {
                try
                {
                    var result = await service.GetPagedEvents(request, cancellationToken);
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
            .AllowAnonymous()
            .WithName("GetPaginatedEvents")
            .WithTags("Event")
            .WithSummary("Paginated Events List")
            .WithDescription("Fetches events with support for filtering, sorting, and pagination.")
            .Produces<PagedResponse<V2Events>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetAllEventSlot(this RouteGroupBuilder group)
        {
            group.MapGet("/slots", async Task<Results<Ok<List<V2Slot>>, ProblemHttpResult>> (
            IV2EventService service,
            CancellationToken cancellationToken = default
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
            .AllowAnonymous()
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
            group.MapGet("/getexpiredevents", async Task<Results<Ok<IEnumerable<V2Events>>, ProblemHttpResult>>
            (
                IV2EventService service,
                CancellationToken cancellationToken = default
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
            .AllowAnonymous()
            .WithName("GetExpiredEvents")
            .WithTags("Event")
            .WithSummary("Get Expired Events")
            .WithDescription("Returns events where EndDate is before today.")
            .Produces<IEnumerable<V2Events>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapReorderEvents(this RouteGroupBuilder group)
        {
            group.MapPost("/reorderslots", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>>
            (
            EventSlotReorderRequest dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken = default
            ) =>
            {
                try
                {
                    if (dto.SlotAssignments == null || dto.SlotAssignments.Count != 6)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Exactly 6 slot assignments must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (dto.SlotAssignments.Select(s => s.SlotNumber).Distinct().Count() != 6 ||
                        dto.SlotAssignments.Any(s => s.SlotNumber < 1 || s.SlotNumber > 6))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Slot numbers must be unique and between 1 and 6.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    if (uid == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    dto.UserId = uid;
                    var result = await service.ReorderEventSlotsAsync(dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
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
            .WithName("ReorderFeaturedEventSlots")
            .WithTags("Event")
            .WithSummary("Reorder Featured Event Slots")
            .WithDescription("Reorders featured event slots using authenticated user.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            group.MapPost("/reorderslotsbyuserid", async Task<Results<
            Ok<string>,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>>
            (
            EventSlotReorderRequest dto,
            IV2EventService service,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.UserId))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    if (dto.SlotAssignments == null || dto.SlotAssignments.Count != 6)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Exactly 6 slot assignments must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (dto.SlotAssignments.Select(s => s.SlotNumber).Distinct().Count() != 6 ||
                        dto.SlotAssignments.Any(s => s.SlotNumber < 1 || s.SlotNumber > 6))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Slot numbers must be unique and between 1 and 6.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.ReorderEventSlotsAsync(dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
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
            .ExcludeFromDescription()
            .WithName("ReorderFeaturedEventSlotsByUserId")
            .WithTags("Event")
            .WithSummary("Reorder Featured Event Slots (Manual/UserId)")
            .WithDescription("Reorders featured event slots using UserId from the payload.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetEventsByStatus(this RouteGroupBuilder group)
        {
            group.MapGet("/getbystatus", async Task<Results<
                Ok<List<V2Events>>,
                BadRequest<ProblemDetails>>>
            (
                [FromQuery] EventStatus status,
                [FromServices] IV2EventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var events = await service.GetEventsByStatus(status, cancellationToken);
                    return TypedResults.Ok(events);
                }
                catch (Exception ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Failed to retrieve events by status",
                        Detail = ex.Message
                    });
                }
            })
                .AllowAnonymous()
                .WithName("GetEventsByStatus")
                .WithTags("Event")
                .WithSummary("Get Events By Status")
                .WithDescription("Retrieves events filtered by their status.")
                .Produces<List<V2Events>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetByStatus(this RouteGroupBuilder group)
        {
            group.MapGet("/getbyfeaturedstatus", async Task<Results<
                Ok<List<V2Events>>,
                BadRequest<ProblemDetails>>>
            (
                [FromQuery] EventStatus status,
                [FromServices] IV2EventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var events = await service.GetEventStatus(status, cancellationToken);
                    return TypedResults.Ok(events);
                }
                catch (Exception ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Failed to retrieve events by status",
                        Detail = ex.Message
                    });
                }
            })
                .AllowAnonymous()
                .WithName("GetFeaturedEventsByStatus")
                .WithTags("Event")
                .WithSummary("Get Featured Events By Status")
                .WithDescription("Retrieves events filtered by their status.")
                .Produces<List<V2Events>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapUpdateFeaturedEvent(this RouteGroupBuilder group)
        {
            group.MapPost("/updatefeaturedevent", async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            NotFound<ProblemDetails>,
            ProblemHttpResult>>
            (
            UpdateFeaturedEvent dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken = default
            ) =>
            {
                try
                {
                    if (dto.EventId == Guid.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "EventId cannot be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    if (uid == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                    dto.UpdatedBy = uid;
                    await service.UpdateFeaturedEvent(dto, cancellationToken);
                    return TypedResults.Ok("Featured event updated successfully.");
                }
                catch (InvalidDataException ex)
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
            .WithName("UpdateFeaturedEvent")
            .WithTags("Event")
            .WithSummary("Update Featured Event")
            .WithDescription("Updates the featured event slot.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            group.MapPost("/updatefeaturedeventbyuserid", async Task < Results <
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult> >
                (
                UpdateFeaturedEvent dto,
                IV2EventService service,
                CancellationToken cancellationToken = default
                ) =>
            {
                try
                {
                    if (dto.EventId == Guid.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "EventId cannot be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    if (string.IsNullOrWhiteSpace(dto.UpdatedBy))
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UpdatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    await service.UpdateFeaturedEvent(dto, cancellationToken);
                    return TypedResults.Ok("Featured event updated successfully.");
                }
                catch (InvalidDataException ex)
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
                .ExcludeFromDescription()
                .WithName("UpdateFeaturedEventByUserId")
                .WithTags("Event")
                .WithSummary("Update Featured Event By UserId")
                .WithDescription("Updates the featured event slot using UserId from the payload.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapUnfeatureEventEndpoint(this RouteGroupBuilder group)
        {
            group.MapPut("/unfeature/{id:guid}", async Task<Results<Ok<string>, NotFound<ProblemDetails>, ProblemHttpResult>> (
                Guid id,
                IV2EventService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.UnfeatureEvent(id, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("UnfeatureEvent")
            .WithTags("Event")
            .WithSummary("Unfeature an Event")
            .WithDescription("Marks an event as not featured and removes it from its slot.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
    }
}
