using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.IService.AdService;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.AdServiceEndPoints
{
    public static class AdServiceEndPoints
    {
        public static RouteGroupBuilder MapAddAdCategoryEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/adcategory", async Task<IResult> (AdCategory dto, IAdService service) =>
            {
                try
                {
                    var result = await service.AddAdCategory(dto);
                    return TypedResults.Created($"/adcategory/{result.Key}", result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("CreateAdCategory")
            .WithTags("AdCategory")
            .WithSummary("Create a new Ad Category")
            .WithDescription("Adds a new ad category record to Dapr Redis state store.");
            return group;
        }

        public static RouteGroupBuilder MapGetAllAdCategoryEndPoints(this RouteGroupBuilder group)
        {
            group.MapGet("/adcategories", async (IAdService service) =>
            {
                try
                {
                    var result = await service.GetAllAdCategory();
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetAllAdCategories")
            .WithTags("AdCategory")
            .WithSummary("Get all Ad Categories")
            .WithDescription("Fetches all ad category records from Dapr state.");

            return group;
        }
    }
}
