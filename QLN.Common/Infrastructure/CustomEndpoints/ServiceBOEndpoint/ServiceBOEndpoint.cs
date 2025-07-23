using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ServiceBOEndpoint
{
    public static class ServiceBOEndpoint
    {
        public static RouteGroupBuilder MapServiceAdGetAllEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/getallbo", async (
                IServicesBoService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetAllServiceBoAds(cancellationToken);
                    return Results.Ok(result); 
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, title: "Internal Server Error");
                }
            })
            .WithName("GetAllServiceAdsBo")
            .WithTags("ServicesBo")
            .WithSummary("Get all service ads")
            .WithDescription("Retrieves a summary of all service ads with basic information including " +
                             "photo uploads, user details, category information, status, dates, and payment transaction ID.")
            .Produces<List<ServiceAdSummaryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

    }
}
