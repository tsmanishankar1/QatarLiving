using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    // QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
    public static class V2CommunityEndpoints
    {
        public static RouteGroupBuilder MapCommunityEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/createPost", async Task<Results<
                    Ok<string>,
                    ForbidHttpResult,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
            (
                V2CommunityPostDto dto,
                IV2CommunityPostService service,
                HttpContext httpContext,
                CancellationToken ct
            ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    dto.UserName = userData.GetProperty("name").GetString();
                    dto.UpdatedBy = uid;
                    dto.UpdatedDate = DateTime.UtcNow;
                    dto.DateCreated = DateTime.UtcNow;

                    var result = await service.CreateCommunityPostAsync(uid, dto, ct);
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
            .WithName("CreateCommunityPost")
            .WithTags("V2Community")
            .WithSummary("Create Community Post")
            .WithDescription("Creates a community post and stores image in blob. Returns status message.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Internal for Dapr invocation:
            group.MapPost("/createPostInternal", async Task<Results<
                    Ok<string>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
            (
                V2CommunityPostDto dto,
                IV2CommunityPostService service,
                CancellationToken ct
            ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.UpdatedBy))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserName and UpdatedBy are required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateCommunityPostAsync(dto.UpdatedBy, dto, ct);
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
            .WithName("CreateCommunityPostInternal")
            .WithTags("V2Community");

            return group;
        }
    }

}