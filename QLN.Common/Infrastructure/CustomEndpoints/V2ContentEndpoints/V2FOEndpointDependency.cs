using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2FOEndpointDependency
    {
        public static RouteGroupBuilder MapFOEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapGetAllFOFeaturedEventEndpoints()
                .MapGetFOEventEndpoints()
                .MapGetEventBySlugEndpoint()
                .MapGetFOPaginatedEvents();
                return group;
        }
    }
}
