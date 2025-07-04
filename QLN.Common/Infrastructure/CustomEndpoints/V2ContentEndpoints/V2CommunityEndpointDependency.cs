using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2CommunityEndpointDependency
    {
        public static RouteGroupBuilder MapCommunityPostEndpoints(this RouteGroupBuilder group)
        {
            group.MapCommunityEndpoints()
                .MapCategoryEndpoints();
            return group;

        }
    }
}
