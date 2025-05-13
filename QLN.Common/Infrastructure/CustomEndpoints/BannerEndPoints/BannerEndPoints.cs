using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("CreateBanner").WithTags("Banner")
            .WithSummary("Create a new banner")
            .WithDescription("Adds a new banner record to Dapr state store.");            
            return group;
        }
        public static RouteGroupBuilder MapUpdateBannerEndPoints(this RouteGroupBuilder group)
        {
            group.MapPut("/banner/{id:Guid}", async Task<IResult> (Guid id, BannerDto dto, IBannerService service) =>
            {
                try
                {
                    var result = await service.UpdateBanner(id, dto);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
           .WithName("UpdateBanner").WithTags("Banner")
           .WithSummary("Update a banner")
           .WithDescription("Updates a banner by ID.")
           .RequireAuthorization();

            return group;
        }

        public static RouteGroupBuilder MapGetByIdBannerEndPoints(this RouteGroupBuilder group)
        {
            group.MapGet("/banner/{id:Guid}", async (Guid id, IBannerService service) =>
            {
                var result = await service.GetBanner(id);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
               .WithName("GetBanner").WithTags("Banner")
               .WithSummary("Get banner by ID")
               .WithDescription("Fetches a banner using its ID.");

            return group;
        }

        public static RouteGroupBuilder MapGetAllBannerEndPoints(this RouteGroupBuilder group)
        {
            group.MapGet("/banners", async (IBannerService service) =>
            {
                try
                {
                    var banners = await service.GetAllBanners();
                    return Results.Ok(banners);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetAllBanners")
            .WithTags("Banner")
            .WithSummary("Get all banners")
            .WithDescription("Fetches all banner records from Dapr state store.");

            return group;
        }


        public static RouteGroupBuilder MapDeleteByIdBannerEndPoints(this RouteGroupBuilder group)
        {
            group.MapDelete("/banner/{id:Guid}", async (Guid id, IBannerService service) =>
            {
                var result = await service.DeleteBanner(id);
                return result ? Results.Ok(true) : Results.NotFound($"Banner with id {id} not found.");
            })
               .WithName("DeleteBanner").WithTags("Banner")
               .WithSummary("Delete banner")
               .WithDescription("Deletes a banner by ID.")
               .RequireAuthorization();

            return group;
        }

        public static RouteGroupBuilder MapUploadImageEndPoints(this RouteGroupBuilder group)
        {
            group.MapPost("/banner/image", async ([FromForm] BannerImageUploadRequest form, IBannerService service) =>
            {
                try
                {
                    var image = await service.UploadImage(form);
                    return Results.Ok(image);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("UploadBannerImage")
            .WithTags("BannerImage")
            .WithSummary("Upload image to banner")
            .WithDescription("Uploads a banner image from multipart form and stores in Dapr.")
            .Accepts<BannerImageUploadRequest>("multipart/form-data")
            .Produces<List<BannerImage>>(StatusCodes.Status200OK)
            .DisableAntiforgery();           
            return group;
        }

        public static RouteGroupBuilder MapGetImageByIdEndPoints(this RouteGroupBuilder group)
        {
            group.MapGet("/banner/image/{id:Guid}", async (Guid id, IBannerService service) =>
            {
                var result = await service.GetImage(id);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }).WithName("GetImageById").WithTags("BannerImage")
         .WithSummary("Get image by ID")
         .WithDescription("Fetches image details from Dapr state by ID.");

            return group;
        }

        public static RouteGroupBuilder MapGetAllImageEndPoints(this RouteGroupBuilder group)
        {
            group.MapGet("/banner/images", async (IBannerService service) =>
            {
                var list = await service.GetAllImages();
                return Results.Ok(list);
            }).WithName("GetAllImages").WithTags("BannerImage")
         .WithSummary("Get all images")
         .WithDescription("Fetches all banner images from Dapr state store.");

            return group;
        }
        public static RouteGroupBuilder MapUpdateBannerImageEndpoints(this RouteGroupBuilder group)
        {
            group.MapPut("/banner/image/{id:Guid}", async (
                Guid id,
                [FromForm] BannerImageUpdateDto dto,
                IBannerService service) =>
            {
                try
                {
                    var updated = await service.UpdateImage(id, dto);
                    return Results.Ok(updated);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
                .WithName("UpdateBannerImage")
                .WithTags("BannerImage")
                .WithSummary("Update existing banner image fields")
                .WithDescription("Updates banner image details and optionally replaces the image file if a new one is provided.")
                .Produces<BannerImage>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .DisableAntiforgery();                

            return group;
        }

        public static RouteGroupBuilder MapDeleteBannerImageEndpoint(this RouteGroupBuilder group)
        {
            group.MapDelete("/banner/image/{id:Guid}", async (
                Guid id,
                IBannerService service) =>
            {
                try
                {
                    var result = await service.DeleteImage(id);
                    return result ? Results.Ok(true) : Results.NotFound();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
                .WithName("DeleteBannerImage")
                .WithTags("BannerImage")
                .WithSummary("Delete a banner image")
                .WithDescription("Deletes the image and removes it from Dapr index. ")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization();

            return group;
        }
    }
}
