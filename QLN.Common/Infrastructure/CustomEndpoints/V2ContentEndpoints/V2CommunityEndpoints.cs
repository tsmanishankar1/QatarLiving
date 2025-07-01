using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QLN.Common.DTO_s.CommunityBo;
using QLN.Common.Infrastructure.IService.V2IContent;
using Microsoft.AspNetCore.Builder;
using static QLN.Common.DTO_s.LocationDto;
using Newtonsoft.Json;
using Google.Type;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2CommunityEndpoints
    {
        public static RouteGroupBuilder MapCreateEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getAllForumCategories", static async Task<Results<Ok<ForumCategoryListDto>, ProblemHttpResult>> (
      V2IContentCommunity service,
      CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var categories = await service.GetAllForumCategoriesAsync(cancellationToken);
                    return TypedResults.Ok(categories);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
  .WithName("GetAllForumCategories")
  .WithTags("Forum")
  .WithSummary("Get All Forum Categories")
  .WithDescription("Returns all forum categories as list.")
  .Produces<ForumCategoryListDto>(StatusCodes.Status200OK)
  .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            return group;
        }
        public static RouteGroupBuilder MapLocationEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getAllZones", static async Task<Results<Ok<LocationZoneListDto>, ProblemHttpResult>> (
                V2IContentCommunity service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var zones = await service.GetAllZonesAsync(cancellationToken);
                    return TypedResults.Ok(zones);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
            .WithName("GetAllZones")
            .WithTags("Location")
            .WithSummary("Get All Zones")
            .WithDescription("Returns all zones as a list.")
            .Produces<LocationZoneListDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapLocationCordinateEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/findAddress", async Task<Results<Ok<List<string>>, ProblemHttpResult>> (
               [FromQuery] int? zone,
               [FromQuery] int? street,
               [FromQuery] int? building,
               [FromQuery] string? location,
               V2IContentCommunity service, // Inject the service to call the method
               CancellationToken cancellationToken = default) =>
            {
                try
                {
                    // Call the service method to get the address coordinates
                    var response = await service.GetAddressCoordinatesAsync(zone, street, building, location, cancellationToken);

                    // Check if the response is valid
                    if (response != null && response.Coordinates != null)
                    {
                        return TypedResults.Ok(response.Coordinates); // Return the coordinates as a List<string>
                    }
                    else
                    {
                        return TypedResults.Problem("Error fetching data from live API or internal service");
                    }
                }
                catch (Exception ex)
                {
                    // Catch any exceptions and return an internal server error response
                    return TypedResults.Problem("Internal Server Error", ex.Message);
                }
            })
           .WithName("FindAddress")
           .WithTags("Location")
           .WithSummary("Find address by zone, street, building, and location")
           .WithDescription("Returns the latitude and longitude of the address based on the provided parameters.")
           .Produces<List<string>>(StatusCodes.Status200OK) // Define the expected response type
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        
    }

      }
}
