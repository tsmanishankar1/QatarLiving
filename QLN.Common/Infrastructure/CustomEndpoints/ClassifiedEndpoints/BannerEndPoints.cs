using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.BannerEndPoints
{
    public static class BannerEndPoints
    {
        public static RouteGroupBuilder MapCreateBannerEndPoints(this RouteGroupBuilder group)
        {
            group.MapPost("/banner", async Task<IResult> (BannerDto dto, IBannerService service) =>
            {
                try
                {
                    var result = await service.CreateBanner(dto);
                    return TypedResults.Created();
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
                    return TypedResults.Problem(
                           title: "Internal Server Error",
                           detail: "An unexpected error occurred.",
                           statusCode: StatusCodes.Status500InternalServerError
                           );
                }
            })
            .WithName("CreateBanner").WithTags("Banner")
            .WithSummary("Create a new banner")
            .WithDescription("Adds a new banner record to Dapr state store.")
            .Produces<Banner>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
            return group;
        }
        public static RouteGroupBuilder MapUpdateBannerEndPoints(this RouteGroupBuilder group)
        {
            group.MapPut("/banner/{id:guid}",
                async Task<Results<
                    Ok<Banner>,
                    NotFound<ProblemDetails>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
                    (
                    [FromRoute] Guid id,
                    [FromBody] BannerDto dto,
                    [FromServices] IBannerService service
                    ) =>
                {
                    try
                    {
                        var result = await service.UpdateBanner(id, dto);
                        return TypedResults.Ok(result);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Banner Not Found",
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
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: "An unexpected error occurred.",
                            statusCode: StatusCodes.Status500InternalServerError
                            );
                    }
                })
                .WithName("UpdateBanner")
                .WithTags("Banner")
                .WithSummary("Update a banner")
                .WithDescription("Updates a banner by ID.")
                .Produces<Banner>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();
            return group;
        }

        public static RouteGroupBuilder MapGetByIdBannerEndPoints(this RouteGroupBuilder group)
        {
            group.MapGet("/banner/{id:Guid}",
                async Task<Results<
                    Ok<Banner>,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>>
                    (
                    [FromRoute] Guid id,
                    [FromServices] IBannerService service
                    ) =>
                {
                    try
                    {
                        var result = await service.GetBanner(id);
                        if (result == null)
                        {
                            return TypedResults.NotFound(new ProblemDetails
                            {
                                Title = "Banner Not Found",
                                Detail = $"No banner found with ID: {id}",
                                Status = StatusCodes.Status404NotFound
                            });
                        }
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: "An unexpected error occurred.",
                            statusCode: StatusCodes.Status500InternalServerError
                            );
                    }
                })
               .WithName("GetBanner").WithTags("Banner")
               .WithSummary("Get banner by ID")
               .WithDescription("Fetches a banner using its ID.")
               .Produces<Banner>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapGetAllBannerEndPoints(this RouteGroupBuilder group)
        {
            group.MapGet("/banners",
                async Task<Results<
                    Ok<IEnumerable<Banner>>,
                    ProblemHttpResult>>
                    (
                    [FromServices] IBannerService service
                    ) =>
                {
                    try
                    {
                        var banners = await service.GetAllBanners();
                        return TypedResults.Ok(banners);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: "Failed to retrieve banners.",
                            statusCode: StatusCodes.Status500InternalServerError
                            );
                    }
                })
                .WithName("GetAllBanners")
                .WithTags("Banner")
                .WithSummary("Get all banners")
                .WithDescription("Fetches all banner records from Dapr state store.")
                .Produces<Banner>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }


        public static RouteGroupBuilder MapDeleteByIdBannerEndPoints(this RouteGroupBuilder group)
        {
            group.MapDelete("/banner/{id:guid}",
                async Task<Results<
                    Ok,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>>
                    (
                    [FromRoute] Guid id,
                    [FromServices] IBannerService service
                    ) =>
                {
                    try
                    {
                        var result = await service.DeleteBanner(id);
                        if (!result)
                        {
                            return TypedResults.NotFound(new ProblemDetails
                            {
                                Title = "Banner Not Found",
                                Detail = $"No banner found with ID: {id}",
                                Status = StatusCodes.Status404NotFound
                            });
                        }
                        return TypedResults.Ok();
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: "An error occurred while deleting the banner.",
                            statusCode: StatusCodes.Status500InternalServerError
                            );
                    }
                })
               .WithName("DeleteBanner").WithTags("Banner")
               .WithSummary("Delete banner")
               .WithDescription("Deletes a banner by ID.")
               .Produces<Banner>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
               .RequireAuthorization();

            return group;
        }

        public static RouteGroupBuilder MapUploadImageEndPoints(this RouteGroupBuilder group)
        {
            group.MapPost("/banner/image",
                async Task<Results<
                    Ok<List<BannerImage>>,
                    BadRequest<ProblemDetails>,
                    ProblemHttpResult>>
                    (
                    [FromForm] BannerImageUploadRequest form,
                    [FromServices] IBannerService service
                    ) =>
                {
                    try
                    {
                        var image = await service.UploadImage(form);
                        return TypedResults.Ok(image);
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
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: "An error occurred while uploading the image.",
                            statusCode: StatusCodes.Status500InternalServerError);
                    }
                })
            .WithName("UploadBannerImage")
            .WithTags("BannerImage")
            .WithSummary("Upload image to banner")
            .WithDescription("Uploads a banner image from multipart form and stores in Dapr.")
            .Accepts<BannerImageUploadRequest>("multipart/form-data")
            .Produces<List<BannerImage>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();
            return group;
        }

        public static RouteGroupBuilder MapGetImageByIdEndPoints(this RouteGroupBuilder group)
        {
            group.MapGet("/banner/image/{id:guid}",
                async Task<Results<
                    Ok<BannerImage>,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>>
                    (
                    [FromRoute] Guid id,
                    [FromServices] IBannerService service
                    ) =>
                {
                    try
                    {
                        var result = await service.GetImage(id);
                        if (result == null)
                        {
                            return TypedResults.NotFound(new ProblemDetails
                            {
                                Title = "Image Not Found",
                                Detail = $"No image found with ID: {id}",
                                Status = StatusCodes.Status404NotFound
                            });
                        }
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: "An unexpected error occurred while fetching the image.",
                            statusCode: StatusCodes.Status500InternalServerError
                            );
                    }
                }).WithName("GetImageById").WithTags("BannerImage")
                .WithSummary("Get image by ID")
                .WithDescription("Fetches image details from Dapr state by ID.")
                .Produces<BannerImage>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }

        public static RouteGroupBuilder MapGetAllImageEndPoints(this RouteGroupBuilder group)
        {
            group.MapGet("/banner/images",
                async Task<Results<
                    Ok<List<BannerImage>>,
                    ProblemHttpResult>>
                    (
                    [FromServices] IBannerService service
                    ) =>
                {
                    try
                    {
                        var list = await service.GetAllImages();
                        return TypedResults.Ok(list.ToList());
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                    title: "Internal Server Error",
                    detail: "An error occurred while retrieving banner images.",
                    statusCode: StatusCodes.Status500InternalServerError);
                    }
                }).WithName("GetAllImages").WithTags("BannerImage")
                .WithSummary("Get all images")
                .WithDescription("Fetches all banner images from Dapr state store.")
                    .Produces<List<BannerImage>>(StatusCodes.Status200OK)
                    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapUpdateBannerImageEndpoints(this RouteGroupBuilder group)
        {
            group.MapPut("/banner/image/{id:guid}",
                async Task<Results<
                Ok<BannerImage>,
                NotFound<ProblemDetails>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
                (
                    [FromRoute] Guid id,
                    [FromForm] BannerImageUpdateDto dto,
                    [FromServices] IBannerService service
                    ) =>
                {
                    try
                    {
                        var updated = await service.UpdateImage(id, dto);
                        if (updated == null)
                        {
                            return TypedResults.NotFound(new ProblemDetails
                            {
                                Title = "Image Not Found",
                                Detail = $"No image found with ID: {id}",
                                Status = StatusCodes.Status404NotFound
                            });
                        }
                        return TypedResults.Ok(updated);
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                })
                .WithName("UpdateBannerImage")
                .WithTags("BannerImage")
                .WithSummary("Update existing banner image fields")
                .WithDescription("Updates banner image details and optionally replaces the image file if a new one is provided.")
                .Produces<BannerImage>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .Produces(StatusCodes.Status404NotFound)
                .DisableAntiforgery()
                .RequireAuthorization();

            return group;
        }

        public static RouteGroupBuilder MapDeleteBannerImageEndpoint(this RouteGroupBuilder group)
        {
            group.MapDelete("/banner/image/{id:guid}",
                async Task<Results<
                    Ok,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>>
                    (
                    [FromRoute] Guid id,
                    [FromServices] IBannerService service
                    ) =>
                {
                    try
                    {
                        var result = await service.DeleteImage(id);
                        if (!result)
                        {
                            return TypedResults.NotFound(new ProblemDetails
                            {
                                Title = "Image Not Found",
                                Detail = $"No banner image found with ID: {id}",
                                Status = StatusCodes.Status404NotFound
                            });
                        }
                        return TypedResults.Ok();
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.Problem(
                            title: "Internal Server Error",
                            detail: "An error occurred while deleting the banner image.",
                            statusCode: StatusCodes.Status500InternalServerError);
                    }
                })
                .WithName("DeleteBannerImage")
                .WithTags("BannerImage")
                .WithSummary("Delete a banner image")
                .WithDescription("Deletes the image and removes it from Dapr index. ")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            return group;
        }
    }
}
