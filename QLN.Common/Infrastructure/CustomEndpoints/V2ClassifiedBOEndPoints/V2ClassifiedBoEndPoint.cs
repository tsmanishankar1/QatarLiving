using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ClassifiedBOEndPoints
{
    public static class V2ClassifiedBoEndPoint
    {
        public static RouteGroupBuilder MapClassifiedBoEndpoints(this RouteGroupBuilder group)
        {

            group.MapGet("lookup/l1-categories/{vertical}", async Task<IResult> (
      string vertical,
      [FromServices]V2IClassifiedBoLandingService service,
      CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetL1CategoriesByVerticalAsync(vertical, token);
                    return TypedResults.Ok(result); 
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
  .WithName("GetL1CategoriesByVertical")
  .WithTags("ClassifiedBo")
  .WithSummary("Get L1 categories for a given vertical")
  .WithDescription("Returns a list of L1 categories from the category tree for a vertical. If none found, returns 200 with empty list.")
  .Produces<List<L1CategoryDto>>(StatusCodes.Status200OK)
  .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
  .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/classified-landing/slot", async Task<Results<
 Ok<string>,
 ForbidHttpResult,
 BadRequest<ProblemDetails>,
 ProblemHttpResult>>
(
 V2ClassifiedLandingBoDto dto,
 [FromServices] V2IClassifiedBoLandingService service,
 HttpContext httpContext,
 CancellationToken cancellationToken
) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                    if (string.IsNullOrEmpty(userClaim))
                        return TypedResults.Forbid();

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var userId = userData.GetProperty("uid").GetString();

                    if (string.IsNullOrEmpty(userId))
                        return TypedResults.Forbid();

                    dto.Id = string.IsNullOrWhiteSpace(dto.Id) ? Guid.NewGuid().ToString() : dto.Id;

                    var message = await service.CreateLandingBoItemAsync(userId, dto, cancellationToken);
                    return TypedResults.Ok(message);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Slot Submission",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Unexpected error", ex.Message);
                }
            })
.RequireAuthorization()
.WithName("CreateLandingBoSlotItemWithAuth")
.WithTags("ClassifiedBo")
.WithSummary("Create Slot Entry (Authorized)")
.WithDescription("Creates a classified landing entry for a selected slot. Requires authentication.")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/classified-landing/slotbyid", async Task<Results<
    Ok<string>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    V2ClassifiedLandingBoDto dto,
    [FromServices] V2IClassifiedBoLandingService service,
    CancellationToken cancellationToken
) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Id))
                        dto.Id = Guid.NewGuid().ToString();

                    var message = await service.CreateLandingBoItemAsync(dto.Id, dto, cancellationToken);
                    return TypedResults.Ok(message);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Slot Submission",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Unexpected error", ex.Message);
                }
            })
.ExcludeFromDescription()
.WithName("CreateLandingBoSlotItemById")
.WithTags("ClassifiedBo")
.WithSummary("Create Slot Entry By UserId")
.WithDescription("Creates a classified landing entry using explicit UserId.")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/seasonal-picks", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                SeasonalPicksDto dto,
                V2IClassifiedBoLandingService service,
                HttpContext httpContext,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Forbid();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    dto.UserId = userData.GetProperty("uid").GetString();
                    dto.UserName = userData.GetProperty("name").GetString();

                    if (string.IsNullOrWhiteSpace(dto.UserId))
                    {
                        return TypedResults.Forbid();
                    }

                    var result = await service.CreateSeasonalPick(dto, cancellationToken);
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
                .RequireAuthorization()
                .WithName("CreateSeasonalPick")
                .WithTags("ClassifiedBo")
                .WithSummary("Create Seasonal Pick")
                .WithDescription("Creates a seasonal pick using authenticated user info and returns success message.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/createSeasonalPickById", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                SeasonalPicksDto dto,
                V2IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /createSeasonalPickById");
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.UserId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateSeasonalPick(dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    Console.WriteLine("InvalidDataException inside /createSeasonalPickById");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fatal error inside /createSeasonalPickById. DTO: {JsonSerializer.Serialize(dto)}");
                    Console.WriteLine($"Exception: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .ExcludeFromDescription()
                .WithName("CreateSeasonalPickByUserId")
                .WithTags("ClassifiedBo")
                .WithSummary("Create Seasonal Pick By UserId")
                .WithDescription("Creates a seasonal pick using UserId passed explicitly in the payload.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/getSeasonalPicks", async Task<Results<
                Ok<List<SeasonalPicksDto>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                V2IClassifiedBoLandingService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetSeasonalPicks(cancellationToken);

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path
                    );
                }
            })
                .WithName("GetSeasonalPicks")
                .WithTags("ClassifiedBo")
                .WithSummary("Get all active seasonal picks")
                .WithDescription("Fetches all active seasonal picks sorted by latest updated date.")
                .Produces<List<SeasonalPicksDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/seasonal-picks/slotted", async Task<Results<
                Ok<List<SeasonalPicksDto>>,
                ProblemHttpResult>>
                (
                V2IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetSlottedSeasonalPicks(cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .WithName("GetSlottedSeasonalPicks")
                .WithTags("ClassifiedBo")
                .WithSummary("Get all slotted seasonal picks")
                .WithDescription("Returns only seasonal picks that are assigned to slot positions (1–6).")
                .Produces<List<SeasonalPicksDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            group.MapPut("/seasonal-picks/replace-slot", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                Guid pickId,
                int slot,
                V2IClassifiedBoLandingService service,
                HttpContext httpContext,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                        return TypedResults.Forbid();

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var userId = userData.GetProperty("uid").GetString();

                    if (string.IsNullOrWhiteSpace(userId))
                        return TypedResults.Forbid();

                    var result = await service.ReplaceSlotWithSeasonalPick(userId, pickId, slot, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .RequireAuthorization()
                .WithName("ReplaceSeasonalPickSlot")
                .WithTags("ClassifiedBo")
                .WithSummary("Add seasonal pick into slot and Replace a seasonal pick into a slot (auth required)")
                .WithDescription("Replaces a seasonal pick into a slot using authenticated user info. Clears any previous slot content.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/replaceSeasonalPickSlot", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                Guid pickId,
                int slot,
                string userId,
                V2IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /replaceSeasonalPickSlot");
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the query or payload.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.ReplaceSlotWithSeasonalPick(userId, pickId, slot, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception inside /internal/replaceSeasonalPickSlot: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .ExcludeFromDescription()
                .WithName("ReplaceSlotWithSeasonalPickByUserId")
                .WithTags("ClassifiedBo")
                .WithSummary("Replace pick slot by UserId")
                .WithDescription("Add seasonal pick into slot and Replaces a seasonal pick into a slot using explicitly passed userId (no auth).")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/seasonal-picks/reorder-slots", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                SeasonalPickSlotReorderRequest request,
                V2IClassifiedBoLandingService service,
                HttpContext httpContext,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim)) return TypedResults.Forbid();

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var userId = userData.GetProperty("uid").GetString();
                    if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Forbid();

                    request.UserId = userId;

                    if (string.IsNullOrWhiteSpace(request.UserId))
                        throw new ArgumentException("UserId is required...");

                    var result = await service.ReorderSeasonalPickSlots(request, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .RequireAuthorization()
                .WithName("ReorderSeasonalPickSlots")
                .WithTags("ClassifiedBo")
                .WithSummary("Reorder slots for seasonal picks (auth required)")
                .WithDescription("Drag-and-drop reordering of seasonal picks, requires authenticated user.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/reorderSeasonalPickSlots", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                SeasonalPickSlotReorderRequest request,                
                V2IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /reorderSeasonalPickSlots");
                try
                {
                    if (string.IsNullOrWhiteSpace(request.UserId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the query or payload.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.ReorderSeasonalPickSlots(request, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in /internal/reorderSeasonalPickSlots: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .ExcludeFromDescription()
                .WithName("ReorderSlotWithUserId")
                .WithTags("ClassifiedBo")
                .WithSummary("Reorder slots by userId (internal)")
                .WithDescription("Allows slot reordering by explicitly passing userId. Used for Dapr/internal tools.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/seasonal-picks/soft-delete", async Task<Results<
    Ok<string>,
    ForbidHttpResult,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    [FromQuery] string pickId,    
    V2IClassifiedBoLandingService service,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim)) return TypedResults.Forbid();

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var userId = userData.GetProperty("uid").GetString();
                    if (string.IsNullOrWhiteSpace(userId)) return TypedResults.Forbid();

                    if (string.IsNullOrWhiteSpace(pickId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "PickId must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.SoftDeleteSeasonalPick(pickId, userId, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .RequireAuthorization()
                .WithName("SoftDeleteSeasonalPick")
                .WithTags("ClassifiedBo")
                .WithSummary("Soft delete a seasonal pick (auth required)")
                .WithDescription("Marks the seasonal pick as inactive, preserving history. Requires authenticated user.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapDelete("/softDeleteSeasonalPick", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery] string pickId,
                [FromQuery] string userId,
                V2IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /softDeleteSeasonalPick");

                try
                {
                    if (string.IsNullOrWhiteSpace(pickId) || string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Both PickId and UserId must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.SoftDeleteSeasonalPick(pickId, userId, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in /softDeleteSeasonalPick: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .ExcludeFromDescription()
                .WithName("SoftDeleteSeasonalPickInternal")
                .WithTags("ClassifiedBo")
                .WithSummary("Soft delete seasonal pick by PickId + UserId (internal)")
                .WithDescription("Internal tool support for seasonal pick soft delete. Requires explicit PickId and UserId.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            return group;
        }
    }
}
