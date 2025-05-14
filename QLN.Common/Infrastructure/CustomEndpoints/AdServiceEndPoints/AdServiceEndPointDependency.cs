using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.AdServiceEndPoints
{
    public static class AdServiceEndPointDependency
    {
        public static RouteGroupBuilder MapAdEndpoints(this RouteGroupBuilder group)
        {
            group.MapAddAdCategoryEndpoints()
                .MapGetAllAdCategoryEndPoints();

            return group;
        }
    }
}
