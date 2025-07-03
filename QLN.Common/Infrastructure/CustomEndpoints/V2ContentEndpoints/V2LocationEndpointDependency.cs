using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2LocationEndpointDependency
    {
        public static RouteGroupBuilder MapLocationsEndpoints(this RouteGroupBuilder group)
        {
            group.MapLocationEndpoints()
                .MapLocationCategoryEndpoints()
                .MapLocationCordinateEndpoints();
            return group;
        }
    }
}
