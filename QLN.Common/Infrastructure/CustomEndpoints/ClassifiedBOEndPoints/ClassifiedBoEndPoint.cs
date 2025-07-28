using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsBoIndex;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ClassifiedBOEndPoints
{
    public static class ClassifiedBoEndPoint
    {
        public static RouteGroupBuilder MapClassifiedBoEndpoints(this RouteGroupBuilder group)
        {

            group.MapPost("/createfeaturedcategory", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                FeaturedCategoryDto dto,
                IClassifiedBoLandingService service,
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
                    var userName = userData.GetProperty("name").GetString();

                    if (string.IsNullOrWhiteSpace(userId))
                        return TypedResults.Forbid();



                    var result = await service.CreateFeaturedCategory(userId, userName, dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .RequireAuthorization()
            .WithName("CreateFeaturedCategory")
            .WithTags("ClassifiedBo")
            .WithSummary("Create Featured Category Slot (auth required)")
            .WithDescription("Create a featured category slot using authenticated user info.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/create-category", async Task<IResult> (
               FeaturedCategoryDto dto,
               [FromQuery] string userId,
               [FromQuery] string userName,
               IClassifiedBoLandingService service,
               CancellationToken token) =>
            {
                if (string.IsNullOrWhiteSpace(dto.CategoryName))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Category name must not be empty.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Vertical))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Vertical must be specified (e.g., items, preloved, collectibles, deals).",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                try
                {
                    var id = await service.CreateFeaturedCategory(userId, userName, dto, token);
                    return TypedResults.Ok(id);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "One or more required resources were not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
               .ExcludeFromDescription()
               .WithName("create-category")
               .WithTags("ClassifiedBo")
               .WithSummary("Create a new category with optional fields  (internal)")
               .WithDescription("Creates a new parent or child category in the specified vertical (items, preloved, collectibles, deals) with optional dynamic fields  (internal use)")
               .Produces<Guid>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("getfeaturedcategoriesbyvertical/{vertical}", async Task<IResult> (
                string vertical,
                [FromServices] IClassifiedBoLandingService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetFeaturedCategoriesByVertical(vertical, token);
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
                .WithName("GetFeaturedCategoriesByVertical")
                .WithTags("ClassifiedBo")
                .WithSummary("Get L1 categories for a given vertical")
                .WithDescription("Returns a list of L1 categories from the category tree for a vertical. If none found, returns 200 with empty list.")
                .Produces<List<FeaturedCategory>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/featured-category/reorder-slots", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                LandingBoSlotReorderRequest request,
                IClassifiedBoLandingService service,
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

                    if (string.IsNullOrWhiteSpace(userId))
                        throw new ArgumentException("UserId is required...");

                    var result = await service.ReorderFeaturedCategorySlots(userId, request, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .RequireAuthorization()
                .WithName("ReorderFeaturedCategorySlots")
                .WithTags("ClassifiedBo")
                .WithSummary("Reorder slots for featured category (auth required)")
                .WithDescription("Drag-and-drop reordering of featured category, requires authenticated user.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/reorderfeaturedcategoryslots", async Task<Results<
               Ok<string>,
               BadRequest<ProblemDetails>,
               ProblemHttpResult>>
               (
                [FromQuery] string userId,
                [FromBody]LandingBoSlotReorderRequest request,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
               ) =>
            {
                Console.WriteLine("Hit endpoint: /ReorderFeaturedCategorySlots");
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

                    var result = await service.ReorderFeaturedCategorySlots(userId, request, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in /internal/reorderFeaturedCategorySlots: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
               .ExcludeFromDescription()
               .WithName("ReorderSlot")
               .WithTags("ClassifiedBo")
               .WithSummary("Reorder slots by userId (internal)")
               .WithDescription("Allows slot reordering by explicitly passing userId. Used for Dapr/internal tools.")
               .Produces<string>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPut("/replacefeaturedcategoryslots", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                LandingBoSlotReplaceRequest dto,
                IClassifiedBoLandingService service,
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



                    var result = await service.ReplaceFeaturedCategorySlots(userId, dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .RequireAuthorization()
            .WithName("ReplaceFeaturedCategorySlots")
            .WithTags("ClassifiedBo")
            .WithSummary("Replace Featured Category Slot (auth required)")
            .WithDescription("Replaces a featured category slot using authenticated user info.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/replace-slot", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                [FromQuery] string userId,
                [FromBody]LandingBoSlotReplaceRequest dto,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.ReplaceFeaturedCategorySlots(userId, dto, cancellationToken);
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
                    return TypedResults.Problem(detail: ex.Message, title: "Internal Server Error");
                }
            })
           .ExcludeFromDescription()
           .WithName("replace-slot")
           .WithTags("ClassifiedBo")
           .WithSummary("Replace Featured Category Slot (internal)")
           .WithDescription("Replaces a featured category slot using explicitly passed UserId (internal use).")
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/featured-category-delete", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery] string categoryId,
                string Vertical,
                IClassifiedBoLandingService service,
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

                    if (string.IsNullOrWhiteSpace(categoryId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "PickId must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.DeleteFeaturedCategory(categoryId, userId, Vertical, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .RequireAuthorization()
                .WithName("SoftDeleteFeaturedCategory")
                .WithTags("ClassifiedBo")
                .WithSummary("Soft delete a featured category (auth required)")
                .WithDescription("Marks the featured category as inactive, preserving history. Requires authenticated user.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/deletefeaturedcategory", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery] string categoryId,
                [FromQuery] string? userId,
                [FromQuery] string Vertical,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /softDeleteFeaturedCategory");

                try
                {
                    if (string.IsNullOrWhiteSpace(categoryId) || string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Both PickId and UserId must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.DeleteFeaturedCategory(categoryId, userId, Vertical, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in /softDeleteFeaturedCategory: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .ExcludeFromDescription()
                .WithName("SoftDeleteFeaturedCategoryInternal")
                .WithTags("ClassifiedBo")
                .WithSummary("Soft delete featured category by PickId + UserId (internal)")
                .WithDescription("Internal tool support for featured category soft delete. Requires explicit FeaturedCategoryId and UserId.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/getslottedfeaturedcategory", async Task<Results<
                Ok<List<FeaturedCategory>>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                string vertical,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetSlottedFeaturedCategory(vertical, cancellationToken);
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
                    return TypedResults.Problem(detail: ex.Message, title: "Internal Server Error");
                }
            })
            .WithName("GetSlottedFeaturedCategory")
            .WithTags("ClassifiedBo")
            .WithSummary("Get slotted Featured Category Slot (internal)")
            .WithDescription("Get slotted featured category (internal use).")
            .Produces<List<FeaturedCategory>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/seasonal-picks", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                SeasonalPicksDto dto,
                IClassifiedBoLandingService service,
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
                    var userId = userData.GetProperty("uid").GetString();
                    var userName = userData.GetProperty("name").GetString();

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.Forbid();
                    }
                    
                    var result = await service.CreateSeasonalPick(userId, userName, dto, cancellationToken);
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


            group.MapPost("/createseasonalpickbyid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                SeasonalPicksDto dto,
                [FromQuery] string userId,
                [FromQuery] string userName,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /createSeasonalPickById");
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateSeasonalPick(userId, userName, dto, cancellationToken);
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


            group.MapGet("/getseasonalpicks", async Task<Results<
                Ok<List<SeasonalPicks>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                IClassifiedBoLandingService service,
                HttpContext context,
                string vertical,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetSeasonalPicks(vertical, cancellationToken);

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
                .Produces<List<SeasonalPicks>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/seasonal-picks/slotted", async Task<Results<
                Ok<List<SeasonalPicks>>,
                ProblemHttpResult>>
                (
                IClassifiedBoLandingService service,
                string vertical,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetSlottedSeasonalPicks(vertical, cancellationToken);
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
                .Produces<List<SeasonalPicks>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            group.MapPut("/seasonal-picks/replace-slot", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromBody] ReplaceSeasonalPickSlotRequest dto,
                IClassifiedBoLandingService service,
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

                    var result = await service.ReplaceSlotWithSeasonalPick(userId, dto, cancellationToken);
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

            group.MapPut("/replace-seasonalpickslot", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery] string userId,
                [FromBody] ReplaceSeasonalPickSlotRequest dto,
                IClassifiedBoLandingService service,
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

                    var result = await service.ReplaceSlotWithSeasonalPick(userId, dto, cancellationToken);
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
                IClassifiedBoLandingService service,
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

                    

                    if (string.IsNullOrWhiteSpace(userId))
                        throw new ArgumentException("UserId is required...");

                    var result = await service.ReorderSeasonalPickSlots(userId, request, cancellationToken);
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

            group.MapPut("/reorder-seasonalpickslots", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery] string userId,
                [FromBody]SeasonalPickSlotReorderRequest request,                
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /reorderSeasonalPickSlots");
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

                    var result = await service.ReorderSeasonalPickSlots(userId, request, cancellationToken);
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
                string Vertical,
                IClassifiedBoLandingService service,
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

                    var result = await service.SoftDeleteSeasonalPick(pickId, userId, Vertical, cancellationToken);
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

             
            group.MapDelete("/softdelete-seasonalpick", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery] string pickId,
                [FromQuery] string userId,
                [FromQuery] string vertical,
                IClassifiedBoLandingService service,
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

                    var result = await service.SoftDeleteSeasonalPick(pickId, userId, vertical, cancellationToken);
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

            group.MapPost("/featured-store", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                FeaturedStoreDto dto,
                IClassifiedBoLandingService service,
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
                    var userId = userData.GetProperty("uid").GetString();
                    var userName = userData.GetProperty("name").GetString();

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.Forbid();
                    }

                    var result = await service.CreateFeaturedStore(userId, userName, dto, cancellationToken);
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
                .WithName("CreateFeaturedStore")
                .WithTags("ClassifiedBo")
                .WithSummary("Create Featured Store")
                .WithDescription("Creates a featured store using authenticated user info and returns success message.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/create-featuredstorebyid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                FeaturedStoreDto dto,
                [FromQuery] string userId,
                [FromQuery] string userName,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /createFeaturedStoreById");

                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateFeaturedStore(userId, userName, dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    Console.WriteLine("InvalidDataException inside /createFeaturedStoreById");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fatal error inside /createFeaturedStoreById. DTO: {JsonSerializer.Serialize(dto)}");
                    Console.WriteLine($"Exception: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .ExcludeFromDescription()
                .WithName("CreateFeaturedStoreByUserId")
                .WithTags("ClassifiedBo")
                .WithSummary("Create Featured Store By UserId")
                .WithDescription("Creates a featured store using UserId passed explicitly in the payload.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getfeaturedstores", async Task<Results<
                Ok<List<FeaturedStore>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                IClassifiedBoLandingService service,
                HttpContext context,
                string vertical,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetFeaturedStores(vertical, cancellationToken);

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
                .WithName("GetFeaturedStores")
                .WithTags("ClassifiedBo")
                .WithSummary("Get all active featured stores")
                .WithDescription("Fetches all active featured stores sorted by latest updated date.")
                .Produces<List<FeaturedStore>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/featured-stores/slotted", async Task<Results<
                Ok<List<FeaturedStore>>,
                ProblemHttpResult>>
                (
                IClassifiedBoLandingService service,
                string vertical,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetSlottedFeaturedStores(vertical, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .WithName("GetSlottedFeaturedStores")
                .WithTags("ClassifiedBo")
                .WithSummary("Get all slotted featured stores")
                .WithDescription("Returns only featured stores that are assigned to slot positions (1–6).")
                .Produces<List<FeaturedStore>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPut("/featured-stores/replace-slot", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
               ReplaceFeaturedStoresSlotRequest dto,
                IClassifiedBoLandingService service,
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

                    var result = await service.ReplaceSlotWithFeaturedStore(userId, dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .RequireAuthorization()
                .WithName("ReplaceFeaturedStoreSlot")
                .WithTags("ClassifiedBo")
                .WithSummary("Replace a featured store into a slot (auth required)")
                .WithDescription("Replaces a featured store into a slot using authenticated user info. Clears any previous slot content.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/replace-featuredstoreSlot", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery] string userId,
                [FromBody]ReplaceFeaturedStoresSlotRequest dto,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /replaceFeaturedStoreSlot");

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

                    var result = await service.ReplaceSlotWithFeaturedStore(userId, dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception inside /replaceFeaturedStoreSlot: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .ExcludeFromDescription()
                .WithName("ReplaceSlotWithFeaturedStoreByUserId")
                .WithTags("ClassifiedBo")
                .WithSummary("Replace featured store slot by UserId")
                .WithDescription("Replaces a featured store into a slot using explicitly passed userId (no auth).")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/featured-stores/reorder-slots", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                FeaturedStoreSlotReorderRequest request,
                IClassifiedBoLandingService service,
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


                    var result = await service.ReorderFeaturedStoreSlots(userId, request, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .RequireAuthorization()
                .WithName("ReorderFeaturedStoreSlots")
                .WithTags("ClassifiedBo")
                .WithSummary("Reorder slots for featured stores (auth required)")
                .WithDescription("Drag-and-drop reordering of featured stores, requires authenticated user.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/reorder-featuredstoreslots", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery]string userId,
                [FromBody]FeaturedStoreSlotReorderRequest request,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /reorderFeaturedStoreSlots");

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

                    var result = await service.ReorderFeaturedStoreSlots(userId, request, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in /internal/reorderFeaturedStoreSlots: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .ExcludeFromDescription()
                .WithName("ReorderFeaturedStoreSlotsWithUserId")
                .WithTags("ClassifiedBo")
                .WithSummary("Reorder featured store slots by userId (internal)")
                .WithDescription("Allows featured store slot reordering by explicitly passing userId. Used for Dapr/internal tools.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/featured-stores/soft-delete", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery] string storeId,
                string vertical,
                IClassifiedBoLandingService service,
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

                    if (string.IsNullOrWhiteSpace(storeId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "StoreId must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.SoftDeleteFeaturedStore(storeId, userId, vertical, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .RequireAuthorization()
                .WithName("SoftDeleteFeaturedStore")
                .WithTags("ClassifiedBo")
                .WithSummary("Soft delete a featured store (auth required)")
                .WithDescription("Marks the featured store as inactive, preserving history. Requires authenticated user.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/softdeletefeaturedstore", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                [FromQuery] string storeId,
                [FromQuery] string userId,
                [FromQuery] string vertical,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                Console.WriteLine("Hit endpoint: /softDeleteFeaturedStore");

                try
                {
                    if (string.IsNullOrWhiteSpace(storeId) || string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Both StoreId and UserId must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.SoftDeleteFeaturedStore(storeId, userId, vertical, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in /softDeleteFeaturedStore: {ex.Message}");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .ExcludeFromDescription()
                .WithName("SoftDeleteFeaturedStoreInternal")
                .WithTags("ClassifiedBo")
                .WithSummary("Soft delete featured store by StoreId + UserId (internal)")
                .WithDescription("Internal tool support for featured store soft delete. Requires explicit StoreId and UserId.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/getall-items", static async Task<Results<Ok<ClassifiedsBoItemsResponseDto>, ProblemHttpResult>>
          (
              [FromServices] ISearchService service,
              [FromBody] CommonSearchRequest request,
              CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    var result = await service.GetAllAsync(ConstantValues.IndexNames.ClassifiedsItemsIndex, request);
                    var getall = new ClassifiedsBoItemsResponseDto
                    {
                        TotalCount = result.TotalCount,
                        ClassifiedsItems = result.ClassifiedsItem
                    };
                    return TypedResults.Ok(getall);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
          .WithName("GetAllItemsAds")
          .WithTags("ClassifiedBo")
          .WithSummary("Get all classifieds ads")
          .WithDescription("Retrieves all service ads from the system. " +
                           "This endpoint returns a list of all available classifieds ads, including their details.")
          .Produces<List<ClassifiedsItemsIndex>>(StatusCodes.Status200OK)
          .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/getall-preloved", static async Task<Results<Ok<ClassifiedsBoPrelovedResponseDto>, ProblemHttpResult>>
           (
               [FromServices] ISearchService service,
               [FromBody] CommonSearchRequest request,
               CancellationToken cancellationToken
           ) =>
            {
                try
                {
                    var result = await service.GetAllAsync(ConstantValues.IndexNames.ClassifiedsPrelovedIndex, request);
                    var getall = new ClassifiedsBoPrelovedResponseDto
                    {
                        TotalCount = result.TotalCount,
                        ClassifiedsPreloved = result.ClassifiedsPrelovedItem
                    };
                    return TypedResults.Ok(getall);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
           .WithName("GetAllPrelovedAds")
           .WithTags("ClassifiedBo")
           .WithSummary("Get all classifieds preloved ads")
           .WithDescription("Retrieves all service ads from the system. This endpoint returns a list of all available classifieds preloved ads, including their details.")
           .Produces<ClassifiedsPrelovedIndex>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/getall-collectibles", static async Task<Results<Ok<ClassifiedsBoCollectiblesResponseDto>, ProblemHttpResult>>
            (
              [FromServices] ISearchService service,
              [FromBody] CommonSearchRequest request,
              CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetAllAsync(ConstantValues.IndexNames.ClassifiedsCollectiblesIndex, request);
                    var getall = new ClassifiedsBoCollectiblesResponseDto
                    {
                        TotalCount = result.TotalCount,
                        ClassifiedsCollectibles = result.ClassifiedsCollectiblesItem
                    };
                    return TypedResults.Ok(getall);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllCollectiblesAds")
            .WithTags("ClassifiedBo")
            .WithSummary("Get all classifieds collectibles ads")
            .WithDescription("Retrieves all service ads from the system. " +
                           "This endpoint returns a list of all available classifieds collectibles ads, including their details.")
            .Produces<List<ClassifiedsCollectiblesIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/getall-deals", static async Task<Results<Ok<ClassifiedsBoDealsResponseDto>, ProblemHttpResult>>
            (
              [FromServices] ISearchService service,
              [FromBody] CommonSearchRequest request,
              CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetAllAsync(ConstantValues.IndexNames.ClassifiedsDealsIndex, request);
                    var getall = new ClassifiedsBoDealsResponseDto
                    {
                        TotalCount = result.TotalCount,
                        ClassifiedsDeals = result.ClassifiedsDealsItem
                    };
                    return TypedResults.Ok(getall);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllDealsAds")
            .WithTags("ClassifiedBo")
            .WithSummary("Get all classifieds deals ads")
            .WithDescription("Retrieves all service ads from the system. " +
                           "This endpoint returns a list of all available classifieds deals ads, including their details.")
            .Produces<List<ClassifiedsDealsIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/bulk-action", async Task<Results<
                    Ok<List<ClassifiedsItems>>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult
                >> (
                    BulkActionRequest req,
                    HttpContext httpContext,
                    IClassifiedBoLandingService service,
                    CancellationToken ct
                ) =>
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
                var userName = userData.GetProperty("name").GetString();
                if (uid == null && userName == null)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unauthorized Access",
                        Detail = "User ID or username could not be extracted from token.",
                        Status = StatusCodes.Status403Forbidden
                    });
                }
                if (!req.AdIds.Any())
                    return TypedResults.BadRequest(new ProblemDetails { Title = "No ads selected." });

                if (req.Action == BulkActionEnum.Remove && string.IsNullOrWhiteSpace(req.Reason))
                    return TypedResults.BadRequest(new ProblemDetails { Title = "Reason required for removal." });
                req.UpdatedBy = uid;
                try
                {
                    var result = await service.BulkAction(req, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })
                .RequireAuthorization()
                .WithName("BulkAction")
                .WithTags("ClassifiedBo")
                .WithSummary("Bulk action classifieds")
                .WithDescription("Performs bulk actions (approve, publish, unpublish, unpromote, unfeature, remove) on selected classifieds. " +
                                 "Requires a list of ad IDs and the action to perform. " +
                                 "If removing, a reason must be provided.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/bulk-action-userid", async Task<Results<
               Ok<List<ClassifiedsItems>>,
               BadRequest<ProblemDetails>,
               ProblemHttpResult
           >> (
               BulkActionRequest req,
               HttpContext httpContext,
               IClassifiedBoLandingService service,
               CancellationToken ct
           ) =>
            {
                try
                {
                    if (req.UpdatedBy == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "UpdatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.BulkAction(req, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })
           .ExcludeFromDescription()
           .WithName("BulkActionByUserId")
           .WithTags("ClassifiedBo")
           .WithSummary("Bulk action classifieds")
           .WithDescription("Performs bulk moderation actions (approve, publish, unpublish, unpromote, unfeature, remove) on selected classifieds ads. " +
                            "Requires a list of ad IDs and the action to perform. " +
                            "If removing, a reason must be provided.")
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/items/transactions", async Task<Results<
                            Ok<TransactionListResponseDto>,
                            BadRequest<ProblemDetails>,
                            ProblemHttpResult>>
                        (
                            IClassifiedBoLandingService service,
                            CancellationToken cancellationToken,
                            [FromQuery] int pageNumber = 1,
                            [FromQuery] int pageSize = 25,
                            [FromQuery] string? searchText = null,
                            [FromQuery] string? transactionType = null,
                            [FromQuery] string? dateCreated = null,
                            [FromQuery] string? datePublished = null,
                            [FromQuery] string? dateStart = null,
                            [FromQuery] string? dateEnd = null,
                            [FromQuery] string? status = null,
                            [FromQuery] string? paymentMethod = null,
                            [FromQuery] string sortBy = "CreationDate",
                            [FromQuery] string sortOrder = "desc"
                        ) =>
            {
                try
                {
                    if (pageNumber < 1)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Page number must be greater than 0.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (pageSize < 1 || pageSize > 100)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Page size must be between 1 and 100.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetTransactionsAsync(
                        pageNumber,
                        pageSize,
                        searchText,
                        transactionType,
                        dateCreated,
                        datePublished,
                        dateStart,
                        dateEnd,
                        status,
                        paymentMethod,
                        sortBy,
                        sortOrder,
                        cancellationToken);

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                        .WithName("GetTransactions")
                        .WithTags("Transactions")
                        .WithSummary("Get transactions with filtering")
                        .WithDescription("Get paginated transactions with search and filter capabilities")
                        .Produces<TransactionListResponseDto>(StatusCodes.Status200OK)
                        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            return group;
        }
    }
}
