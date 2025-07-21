using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService;
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
                    return TypedResults.Ok(result); // ✅ Always returns 200 with result (even if it's empty)
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



            return group;
            }
        }


}
