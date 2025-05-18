using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.SaveSearchEndPoints
{
    public static class SearchEndPoints
    {
        public static RouteGroupBuilder MapSaveSearchEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/search", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                SaveSearchRequestDto dto,
                ISaveSearchService service,
                HttpContext context
            ) =>
            {
                var userId = context.User.GetId();
                dto.UserId = userId;

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search name is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (dto.SearchQuery == null || string.IsNullOrWhiteSpace(dto.SearchQuery.Text))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search query text is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    await service.SaveSearchAsync(dto);
                    return TypedResults.Ok("Search saved successfully.");
                }
                catch (SaveSearchException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Save Search Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
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
            .WithName("SaveSearch")
            .WithTags("Search")
            .WithSummary("Save user search")
            .WithDescription("Save the search criteria to Redis via Dapr.")
            .RequireAuthorization()
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapGetSavedSearchesEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/search", async Task<Results<
                Ok<List<SavedSearchResponseDto>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                HttpContext context,
                ISaveSearchService service
            ) =>
            {
                var userIdObj = context.User.GetId();
                if (userIdObj == null)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid User",
                        Detail = "User ID could not be determined.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                var userId = userIdObj.ToString().ToLower();

                try
                {
                    var searches = await service.GetSearchesAsync(userId);
                    return TypedResults.Ok(searches);
                }
                catch (GetSearchesException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Get Searches Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
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
            .WithName("GetSavedSearches")
            .WithTags("Search")
            .WithSummary("Get saved searches")
            .WithDescription("Get all saved searches for the current user.")
            .RequireAuthorization()
            .Produces<List<SavedSearchResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

    }
}

