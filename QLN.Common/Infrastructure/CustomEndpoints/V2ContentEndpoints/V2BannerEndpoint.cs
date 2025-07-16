using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Subscriptions;
using System.Text.Json;
using static QLN.Common.Infrastructure.Constants.ConstantValues;


namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2BannerEndpoint
    {
        public static RouteGroupBuilder MapCreateBannerTypeEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/createbannertype", async Task<Results<
    Ok<string>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    V2BannerTypeDto dto,
    IV2BannerService service,
    ILogger<IV2BannerService> logger,
    CancellationToken ct
) =>
            {
                try
                {
                    logger.LogInformation("CreateBannerType called");

                    if (dto.VerticalId == 0 || dto.SubVerticalId == 0)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "VerticalId and SubVerticalId are required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateBannerTypeAsync(dto, ct);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "Validation failed");
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while creating banner type");
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("createbannertype")
            .WithTags("Banners")
    .WithSummary("Create Banner Type")
    .WithDescription("Creates a banner type with vertical and sub-vertical mapping.")
   .Produces<string>(StatusCodes.Status200OK)
   .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
   .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;

        }

        public static RouteGroupBuilder MapBannerLocationEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/createlocation", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> (

                V2BannerLocationDto dto,
                IV2BannerService service,
                ILogger<IV2BannerService> logger,
                CancellationToken ct) =>
            {
                try
                {
                    var result = await service.CreateBannerLocationAsync(dto, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create banner location");
                    return TypedResults.Problem("Error creating banner location", ex.Message);
                }
            })
            .WithName("createbannerlocation")
            .WithTags("Banners")
            .WithSummary("Create Banner Location")
           .WithDescription("Creates a banner Location with vertical and sub-vertical mapping.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/createpagelocation", async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ProblemHttpResult>> (

                    V2BannerPageLocationDto dto,
                    IV2BannerService service,
                    ILogger<IV2BannerService> logger,
                    CancellationToken ct) =>
            {
                try
                {
                    var result = await service.CreateBannerPageLocationAsync(dto, ct);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create banner location");
                    return TypedResults.Problem("Error creating banner location", ex.Message);
                }
            })
                .WithName("createbannerpagelocation")
                .WithTags("Banners")
               .WithSummary("Create Banner Page Location")
              .WithDescription("Creates a banner Page Location with vertical and sub-vertical mapping.")
               .Produces<string>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                  .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getlocations", async (
                    IV2BannerService service,
                    CancellationToken ct) =>
                {
                    var result = await service.GetAllBannerLocationsAsync(ct);
                    return TypedResults.Ok(result);
                })
                 .WithName("GetBannerLocation")
     .WithTags("Banners")
    .WithSummary("Get Banner Location")
    .WithDescription("Get a banner Location with vertical and sub-vertical mapping.")
   .Produces<string>(StatusCodes.Status200OK)
   .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
   .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getbyvertical/{verticalId:int}", async (
          [FromServices] IV2BannerService bannerService,
         int verticalId,
            CancellationToken cancellationToken) =>
            {
                if (!Enum.IsDefined(typeof(Vertical), verticalId))
                    return Results.BadRequest("Invalid VerticalId");

                var result = await bannerService.GetBannerTypesByVerticalAsync((Vertical)verticalId, cancellationToken);
                return Results.Ok(result);
            })
       .WithName("GetBannerTypesByVertical")
      .WithTags("Banners")
      .WithDescription("Returns banner types with page and banner names for a given vertical.")
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getbannertypesbysubvertical/{subVerticalId:int}", async (
     int subVerticalId,
     IV2BannerService service,
     CancellationToken cancellationToken) =>
            {
                if (!Enum.IsDefined(typeof(SubVertical), subVerticalId))
                    return Results.BadRequest("Invalid VerticalId");
                var result = await service.GetBannerTypesBySubVerticalAsync((SubVertical)subVerticalId, cancellationToken);
                return Results.Ok(result);
            })
          .WithName("GetDetailedBannerTypesBySubVerticalId")
           .WithTags("Banners")
      .WithDescription("Returns banner types with page and banner names for a given subvertical.")
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            group.MapGet("/getbannertypesbypageid/{pageId:guid}", async (
     Guid pageId,
     IV2BannerService service,
     CancellationToken cancellationToken) =>
            {
                var result = await service.GetBannerTypesByPageIdAsync(pageId, cancellationToken);
                return Results.Ok(result);
            })

            .WithName("GetDetailedBannerTypesByPageId")
           .WithTags("Banners")
      .WithDescription("Returns banner types with page and banner names for a given subvertical.")
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            return group;
        }
        public static RouteGroupBuilder MapCreateBannerEndpoints(this RouteGroupBuilder group)
        {
            // Authenticated Create - Extracts userId from token
            group.MapPost("/create", async Task<Results<
                Ok<string>,
                UnauthorizedHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                V2BannerDto dto,
                IV2BannerService service,
                HttpContext httpContext,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Unauthorized();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    if (string.IsNullOrEmpty(uid))
                    {
                        return TypedResults.Unauthorized();
                    }

                    dto.Createdby = uid;
                    httpContext.Request.Headers["X-User-Id"] = uid;

                    var result = await service.CreateBannerAsync(uid, dto, cancellationToken);
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
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("CreateBanners")
            .WithTags("Banners")
            .WithDescription("Creates banner with authenticated user.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized) // Added 401
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

          
            group.MapPost("/createbyuserid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                V2BannerDto dto,
                IV2BannerService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Createdby))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "CreatedBy is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateBannerAsync(dto.Createdby, dto, cancellationToken);
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
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("CreateBannersByUserId")
            .WithTags("Banners")
            .WithDescription("Creates banner and expects userId explicitly.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapUpdateBannerEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/edit", async Task<Results<
    Ok<string>,
    UnauthorizedHttpResult,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    V2BannerDto dto,
    IV2BannerService service,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Unauthorized();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    if (string.IsNullOrEmpty(uid))
                    {
                        return TypedResults.Unauthorized();
                    }

                    dto.Updatedby = uid;

                    var result = await service.EditBannerAsync(uid, dto, cancellationToken);
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
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
.WithName("EditBanner")
.WithTags("Banners")
.WithDescription("Edits an existing banner with authenticated user.")
.Produces<string>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Internal endpoint
            group.MapPost("/editbyuserid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                V2BannerDto dto,
                IV2BannerService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Updatedby))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "UpdatedBy is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.EditBannerAsync(dto.Updatedby, dto, cancellationToken);
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
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("EditBannerByUserId")
            .WithTags("Banners")
            .WithDescription("Edits a banner and expects userId explicitly.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapDeleteBannerEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/delete", async Task<Results<
    Ok<string>,
    UnauthorizedHttpResult,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    Guid bannerId,
    IV2BannerService service,
    HttpContext httpContext,
    CancellationToken cancellationToken
) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Unauthorized();
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    if (string.IsNullOrEmpty(uid))
                    {
                        return TypedResults.Unauthorized();
                    }

                    var result = await service.DeleteBannerAsync(uid, bannerId, cancellationToken);
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
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
.WithName("DeleteBanner")
.WithTags("Banners")
.WithDescription("Soft-deletes a banner by setting Status = false.")
.Produces<string>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            group.MapPost("/deletebyuserid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                [FromBody] DeleteBannerRequest request,
                IV2BannerService service,
                CancellationToken cancellationToken
            ) =>
            {
                try
                {
                    if (request.BannerId == Guid.Empty || string.IsNullOrWhiteSpace(request.UpdatedBy))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "BannerId and UpdatedBy are required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.DeleteBannerAsync(request.UpdatedBy, request.BannerId, cancellationToken);
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
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .ExcludeFromDescription()
            .WithName("DeleteBannerByUserId")
            .WithTags("Banners")
            .WithDescription("Soft-deletes a banner (internal use only).")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}

