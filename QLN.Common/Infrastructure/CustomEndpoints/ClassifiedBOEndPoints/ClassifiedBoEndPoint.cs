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
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.IService;
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
                            Conflict<ProblemDetails>,
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
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                        .WithName("CreateFeaturedCategory")
                        .WithTags("ClassifiedBo")
                        .WithSummary("Create Featured Category Slot (auth required)")
                        .WithDescription("Create a featured category slot using authenticated user info.")
                        .Produces<string>(StatusCodes.Status200OK)
                        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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
                catch (ConflictException ex)
                {
                    Console.WriteLine("ConflictException inside /createFeaturedCateoryById");
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
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
                         .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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
                .AllowAnonymous()
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
            .AllowAnonymous()
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
                            Conflict<ProblemDetails>,
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
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                            .WithName("CreateSeasonalPick")
                            .WithTags("ClassifiedBo")
                            .WithSummary("Create Seasonal Pick")
                            .WithDescription("Creates a seasonal pick using authenticated user info and returns success message.")
                            .Produces<string>(StatusCodes.Status200OK)
                            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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
                catch (ConflictException ex)
                {
                    Console.WriteLine("ConflictException inside /createSeasonalPickById");
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Duplicate Seasonal Pick",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
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
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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
                .AllowAnonymous()
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
                .AllowAnonymous()
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
                            Conflict<ProblemDetails>,
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
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                            .WithName("CreateFeaturedStore")
                            .WithTags("ClassifiedBo")
                            .WithSummary("Create Featured Store")
                            .WithDescription("Creates a featured store using authenticated user info and returns success message.")
                            .Produces<string>(StatusCodes.Status200OK)
                            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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
                catch (ConflictException ex)
                {
                    Console.WriteLine("ConflictException inside /createFeaturedStoreById");
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Duplicate Featured Store",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
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
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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
                .AllowAnonymous()
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
                .AllowAnonymous()
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
            [FromServices] IClassifiedBoLandingService service,
            [FromBody] GetAllSearch request,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetAllItems(request);
                    return TypedResults.Ok(result);
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
            .Produces<List<ClassifiedItems>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);




            group.MapPost("/getall-collectibles", static async Task<Results<Ok<ClassifiedsBoCollectiblesResponseDto>, ProblemHttpResult>>
            (
            [FromServices] IClassifiedBoLandingService service,
            [FromBody] GetAllSearch request,
            CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var result = await service.GetAllCollectibles(request);
                    return TypedResults.Ok(result);
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
            .Produces<List<ClassifiedCollectibles>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/bulk-items-action", async Task<Results<
                                Ok<string>,
                                BadRequest<ProblemDetails>,
                                Conflict<ProblemDetails>,
                                NotFound<ProblemDetails>,
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
                var userId = uid;
                try
                {
                    var result = await service.BulkItemsAction(req, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "NotFound Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })                            
                            .WithName("BulkItemsAction")
                            .WithTags("ClassifiedBo")
                            .WithSummary("Bulk items action classifieds")
                            .WithDescription("Performs bulk items actions (approve, publish, unpublish, unpromote, unfeature, remove) on selected classifieds. " +
                                             "Requires a list of ad IDs and the action to perform. " +
                                             "If removing, a reason must be provided.")
                            .Produces<string>(StatusCodes.Status200OK)
                            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/bulk-items-action-userid", async Task<Results<
               Ok<string>,
               BadRequest<ProblemDetails>,
               Conflict<ProblemDetails>,
               NotFound<ProblemDetails>,
               ProblemHttpResult
           >> (
               BulkActionRequest req,
               string? userId,
               HttpContext httpContext,
               IClassifiedBoLandingService service,
               CancellationToken ct
           ) =>
            {
                try
                {
                    if (userId == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "UpdatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.BulkItemsAction(req, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "NotFound Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })
           .ExcludeFromDescription()
           .WithName("BulkItemsActionByUserId")
           .WithTags("ClassifiedBo")
           .WithSummary("Bulk items action classifieds")
           .WithDescription("Performs bulk items actions (approve, publish, unpublish, unpromote, unfeature, remove) on selected classifieds ads. " +
                            "Requires a list of ad IDs and the action to perform. " +
                            "If removing, a reason must be provided.")
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/bulk-collectibles-action", async Task<Results<
                   Ok<string>,
                   BadRequest<ProblemDetails>,
                   NotFound<ProblemDetails>,
                   Conflict<ProblemDetails>,
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
                var userId = uid;
                try
                {
                    var result = await service.BulkCollectiblesAction(req, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "NotFound Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })               
               .WithName("BulkCollectiblesAction")
               .WithTags("ClassifiedBo")
               .WithSummary("Bulk collectibles action classifieds")
               .WithDescription("Performs bulk collectibles actions (approve, publish, unpublish, unpromote, unfeature, remove) on selected classifieds. " +
                                "Requires a list of ad IDs and the action to perform. " +
                                "If removing, a reason must be provided.")
               .Produces<string>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
               .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
               .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/bulk-collectibles-action-userid", async Task<Results<
               Ok<string>,
               BadRequest<ProblemDetails>,
               Conflict<ProblemDetails>,
               NotFound<ProblemDetails>,
               ProblemHttpResult
           >> (
               BulkActionRequest req,
               string? userId,
               HttpContext httpContext,
               IClassifiedBoLandingService service,
               CancellationToken ct
           ) =>
            {
                try
                {
                    if (userId == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "UpdatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.BulkCollectiblesAction(req, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "NotFound Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })
           .ExcludeFromDescription()
           .WithName("BulkCollectiblesActionByUserId")
           .WithTags("ClassifiedBo")
           .WithSummary("Bulk collectibles action classifieds")
           .WithDescription("Performs bulk collectibles actions (approve, publish, unpublish, unpromote, unfeature, remove) on selected classifieds ads. " +
                            "Requires a list of ad IDs and the action to perform. " +
                            "If removing, a reason must be provided.")
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/items/transactions", async Task<Results<
                Ok<TransactionListResponseDto>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                [FromBody] TransactionFilterRequestDto request,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (request.PageNumber < 1)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Page number must be greater than 0.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (request.PageSize < 1 || request.PageSize > 100)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Page size must be between 1 and 100.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetTransactionsAsync(request, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                        .WithName("GetTransactions")
                        .WithTags("ClassifiedBo")
                        .WithSummary("Get transactions with filtering")
                        .WithDescription("Get paginated transactions with search and filter capabilities")
                        .Produces<TransactionListResponseDto>(StatusCodes.Status200OK)
                        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("/preloved-ads/payment-summary", async Task<Results<
                Ok<PaginatedResult<PrelovedAdPaymentSummaryDto>>,
                ProblemHttpResult>>
                (
                [AsParameters] PaginationQuery pagination,
                [FromQuery] string? search,
                [FromQuery] string? sortBy,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    var result = await service.GetAllPrelovedAdPaymentSummaries(
                        pagination.PageNumber ?? 1,
                        pagination.PageSize ?? 12,
                        search,
                        sortBy,
                        cancellationToken);

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
                .WithName("GetAllPrelovedAdPaymentSummaries")
                .WithTags("ClassifiedBo")
                .AllowAnonymous()
                .WithSummary("Get all preloved ad payment summaries")
                .WithDescription("Returns paginated list of preloved ads with payment-style summaries including order ID, status, contact info, and subscription type.")
                .Produces<PaginatedResult<PrelovedAdPaymentSummaryDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getallprelovedads", async (
                [FromServices] IClassifiedBoLandingService service,
                CancellationToken cancellationToken,
                [FromQuery] string? sortBy = "CreationDate",
                [FromQuery] string? search = null,
                [FromQuery] DateTime? fromDate = null,
                [FromQuery] DateTime? toDate = null,
                [FromQuery] DateTime? publishedFrom = null,
                [FromQuery] DateTime? publishedTo = null,
                [FromQuery] int? status = null,
                [FromQuery] bool? isPromoted = null,
                [FromQuery] bool? isFeatured = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 12
                ) =>
            {
                try
                {
                    var result = await service.GetAllPrelovedBoAds(sortBy, search, fromDate, toDate, 
                        publishedFrom, publishedTo, status, isFeatured, isPromoted, pageNumber,
                        pageSize, cancellationToken
                    );

                    return Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        title: "Internal Server Error",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetAllPrelovedBoAds")
                .WithTags("ClassifiedBo")
                .AllowAnonymous()
                .WithSummary("Get all preloved ads with pagination")
                .WithDescription("Retrieves a paginated summary of all Preloved ads with optional filters like status, date, promotion and feature state.")
                .Produces<PaginatedResult<PrelovedAdSummaryDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getdealsSummary", async Task<Results<
    Ok<PaginatedResult<DealsAdSummaryDto>>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    IClassifiedBoLandingService service,
    HttpContext context,
    int? pageNumber,
    int? pageSize,
    string? search,
    string? sortBy,
    CancellationToken cancellationToken
) =>
            {
                try
                {
                    var result = await service.GetAllDeals(pageNumber, pageSize, search, sortBy, cancellationToken);
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
.WithName("GetDeals")
.WithTags("ClassifiedBo")
.AllowAnonymous()
.WithSummary("Get all deals")
.WithDescription("Fetches all deals with optional search, pagination, and sorting.")
.Produces<PaginatedResult<DealsAdSummaryDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/DealsViewSummary", async Task<Results<
    Ok<PaginatedResult<DealsViewSummaryDto>>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    IClassifiedBoLandingService service,
    HttpContext context,
    int? pageNumber,
    int? pageSize,
    string? search,
    string? sortBy,
    string? status,
    bool? isPromoted,
    bool? isFeatured,
    CancellationToken cancellationToken
) =>
            {
                try
                {
                    var result = await service.DealsViewSummary(
                        pageNumber, pageSize, search, sortBy, status, isPromoted, isFeatured, cancellationToken);

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
.WithName("DealsViewSummary")
.WithTags("ClassifiedBo")
.AllowAnonymous()
.WithSummary("Get all deals")
.WithDescription("Fetches all deals with optional search, pagination, sorting, and filters like status, isPromoted, isFeatured.")
.Produces<PaginatedResult<DealsAdSummaryDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapDelete("/dealsdelete", async Task<Results<
Ok<string>,
ForbidHttpResult,
BadRequest<ProblemDetails>,
ProblemHttpResult>>
(
[FromBody] DealsBulkDelete deleteRequest,
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


                    if (deleteRequest?.AdId == null || !deleteRequest.AdId.Any())
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "At least one Ad ID must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }


                    var result = await service.SoftDeleteDeals(deleteRequest, userId, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
.WithName("DealsSoftDelete")
.WithTags("ClassifiedBo")
.WithSummary("Soft delete deal ads (auth required)")
.WithDescription("Marks one or more deal ads as inactive. Requires authenticated user.")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapDelete("/softdeletedeals", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                [FromBody] DealsBulkDelete deleteRequest,
                [FromQuery] string? userId,
                IClassifiedBoLandingService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (deleteRequest.AdId == null || !deleteRequest.AdId.Any() || string.IsNullOrWhiteSpace(userId))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Both Ad IDs and UserId are required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.SoftDeleteDeals(deleteRequest, userId, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("DealsSoftDeleteInternal")
            .WithTags("ClassifiedBo")
            .WithSummary("Internal soft delete for deals")
            .WithDescription("Soft deletes deals using Ad IDs and User ID from query.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/bulk-preloved-action", async Task<Results<
Ok<string>,
BadRequest<ProblemDetails>,
NotFound<ProblemDetails>,
Conflict<ProblemDetails>,
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
                var userId = uid;
                try
                {
                    var result = await service.BulkPrelovedAction(req, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "NotFound Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })
.WithName("BulkPrelovedAction")
.WithTags("ClassifiedBo")
.WithSummary("Bulk preloved action classifieds")
.WithDescription("Performs bulk preloved actions (approve, publish, unpublish, unpromote, unfeature, remove) on selected classifieds. " +
        "Requires a list of ad IDs and the action to perform. " +
        "If removing, a reason must be provided.")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
.Produces<ProblemDetails>(StatusCodes.Status409Conflict)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPost("/bulk-preloved-action-userid", async Task<Results<
               Ok<string>,
               BadRequest<ProblemDetails>,
               Conflict<ProblemDetails>,
               NotFound<ProblemDetails>,
               ProblemHttpResult
>> (
               BulkActionRequest req,
               string? userId,
               HttpContext httpContext,
               IClassifiedBoLandingService service,
               CancellationToken ct
           ) =>
            {
                try
                {
                    if (userId == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "UpdatedBy cannot be null.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.BulkPrelovedAction(req, userId, ct);
                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Conflict Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "NotFound Exception",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(ex.Message);
                }
            })
           .ExcludeFromDescription()
           .WithName("BulkPrelovedActionByUserId")
           .WithTags("ClassifiedBo")
           .WithSummary("Bulk preloved action classifieds")
           .WithDescription("Performs bulk preloved actions (approve, publish, unpublish, unpromote, unfeature, remove) on selected classifieds ads. " +
                            "Requires a list of ad IDs and the action to perform. " +
                            "If removing, a reason must be provided.")
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
         
            group.MapGet("/getstoresubscriptions", async Task<Results<
           Ok<List<StoresSubscriptionDto>>,
           BadRequest<ProblemDetails>,
           ProblemHttpResult>>
           (
           IClassifiedBoLandingService service,
           HttpContext context,
           string? subscriptionType,
           string? filterDate,
           CancellationToken cancellationToken
           ) =>
            {
                try
                {
                    var result = await service.getStoreSubscriptions(subscriptionType, filterDate,cancellationToken);

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
           .WithName("GetStoresSubscriptions")
           .AllowAnonymous()
           .WithTags("ClassifiedBo")
           .WithSummary("Get all subscriptions on stores.")
           .WithDescription("Fetches all subscriptions of users on stores")
           .Produces<List<StoresSubscriptionDto>>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/create-stores-subscriptions", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                StoresSubscriptionDto dto,
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

                    var result = await service.CreateStoreSubscriptions(dto, cancellationToken);
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
                .WithName("CreateStoresSubscription")
                .WithTags("ClassifiedBo")
                .WithSummary("Create Stores Subscriptions")
                .WithDescription("Creates a stores subscriptions using authenticated user info and returns success message.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            
            group.MapPost("/create-store-subscriptions", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                StoresSubscriptionDto dto,
                IClassifiedBoLandingService service,         
                CancellationToken cancellationToken
                ) =>
            {
                try
                {
                    Console.WriteLine("hits internal bo");
                    var result = await service.CreateStoreSubscriptions(dto, cancellationToken);
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
                .WithName("CreateStoreSubscription")
                .WithTags("ClassifiedBo")
                .WithSummary("Create Stores Subscriptions")
                .WithDescription("Creates a stores subscriptions using authenticated user info and returns success message.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/edit-stores-subscriptions", async Task<Results<
          Ok<string>,
          ForbidHttpResult,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          IClassifiedBoLandingService service,
          HttpContext httpContext,
          int OrderID,
          string Status,
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


                    var result = await service.EditStoreSubscriptions(OrderID, Status, cancellationToken);

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: httpContext.Request.Path
                    );
                }
            })
           .WithName("EditStoresSubscriptions")
          .WithTags("ClassifiedBo")
          .WithSummary("Edit subscriptions on stores.")
          .WithDescription("Edit the status information of stores subscriptions.")
          .Produces<List<StoresSubscriptionDto>>(StatusCodes.Status200OK)
          .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
          .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            group.MapPost("items/admin/post-by-id", async Task<IResult> (
              ClassifiedsItems dto,
              IClassifiedService service,
              CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var response = await service.CreateClassifiedItemsAd(dto, token);
                    return TypedResults.Created($"/api/classifieds/items/admin/post-by-id/{response.AdId}", response);

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
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
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
              .WithName("AdminPostItemsAdById")
              .WithTags("ClassifiedBo")
              .WithSummary("Post classified items ad using provided UserId, UserName and Email")
              .WithDescription("For admin scenarios where the UserId, UserName and Email is passed.")
              .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/edit-store-subscriptions", async Task<Results<
         Ok<string>,
         BadRequest<ProblemDetails>,
         ProblemHttpResult>>
         (
         IClassifiedBoLandingService service,
         HttpContext httpContext,
         int OrderID,
         string Status,
         CancellationToken cancellationToken
         ) =>
            {
                try
                {
                    

            group.MapPost("preloved/admin/post-by-id", async Task<IResult> (
               ClassifiedsPreloved dto,
               IClassifiedService service,
               CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == null)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateClassifiedPrelovedAd(dto, token);

                    return TypedResults.Created($"/api/classifieds/preloved/admin/post-by-id/{result.AdId}", result);
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
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested resource or reference was not found.",
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
               .WithName("AdminPostPrelovedAdById")
               .WithTags("ClassifiedBo")
               .WithSummary("Post classified preloved ad using provided UserId, UserName and Email")
               .WithDescription("For admin/service scenarios where the UserId, UserName and Email is passed.")
               .Produces<AdCreatedResponseDto>(StatusCodes.Status201Created)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
                    var result = await service.EditStoreSubscriptions(OrderID, Status, cancellationToken);

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: httpContext.Request.Path
                    );
                }
            })
         .ExcludeFromDescription()
         .WithName("EditStoreSubscriptions")
         .WithTags("ClassifiedBo")
         .WithSummary("Edit subscriptions on stores.")
         .WithDescription("Edit the status information of stores subscriptions.")
         .Produces<List<StoresSubscriptionDto>>(StatusCodes.Status200OK)
         .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
         .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/preloved/transactions", async Task<Results<
               Ok<PrelovedTransactionListResponseDto>,
               BadRequest<ProblemDetails>,
               ProblemHttpResult>>
               (
               IClassifiedBoLandingService service,
               CancellationToken cancellationToken,
               [FromQuery] int pageNumber = 1,
               [FromQuery] int pageSize = 25,
               [FromQuery] string? searchText = null,
               [FromQuery] string? dateCreated = null,
               [FromQuery] string? datePublished = null,
               [FromQuery] string? dateStart = null,
               [FromQuery] string? dateEnd = null,
               [FromQuery] string? status = null,
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
                    var result = await service.GetPrelovedTransactionsAsync(
                        pageNumber,
                        pageSize,
                        searchText,
                        dateCreated,
                        datePublished,
                        dateStart,
                        dateEnd,
                        status,
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
.WithName("GetPrelovedTransactions")
.WithTags("ClassifiedBo")
.AllowAnonymous()
.WithSummary("Get transactions with filtering")
.WithDescription("Get paginated preloved transactions with search and filter capabilities")
.Produces<TransactionListResponseDto>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}