using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IServiceBoService;
namespace QLN.Common.Infrastructure.CustomEndpoints.ServiceBOEndpoint
{
    public static class ServiceBOEndpoint
    {
        public static RouteGroupBuilder MapServiceAdGetAllEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getallbo", async (
                IServicesBoService service,
                CancellationToken cancellationToken,
                [FromQuery] string? sortBy = null,
                [FromQuery] string? search = null,
                [FromQuery] DateTime? fromDate = null,
                [FromQuery] DateTime? toDate = null,
                [FromQuery] DateTime? publishedFrom = null,
                [FromQuery] DateTime? publishedTo = null,
                [FromQuery] int? status = null,
                [FromQuery] bool? isPromoted = null,
                [FromQuery] bool? isFeatured = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 12) =>
            {
                try
                {
                    var result = await service.GetAllServiceBoAds(
                        sortBy ?? "CreationDate",
                        search,
                        fromDate,
                        toDate,
                        publishedFrom,
                        publishedTo,
                        status,
                        isFeatured,
                        isPromoted,
                        pageNumber,
                        pageSize,
                        cancellationToken
                    );
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, title: "Internal Server Error");
                }
            })
            .WithName("GetAllServiceAdsBo")
            .WithTags("ServicesBo")
            .WithSummary("Get all service ads with pagination")
            .WithDescription("Retrieves a paginated summary of all service ads with optional sorting, search, status, report, feature and date filters.")
            .Produces<PaginatedResult<ServiceAdSummaryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }




    }
}
