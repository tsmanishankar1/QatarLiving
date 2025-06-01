using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ContentEndpoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class ClassifiedEndPointDependency
    {
        public static RouteGroupBuilder MapContentLandingEndpoints(this RouteGroupBuilder group)
        {
            group.MapContentLandingEndpoint()
                .MapGetContentByIdEndpoint()
                .MapGetEventByIdEndpoint();

            return group;
        }
        public static RouteGroupBuilder MapServicesEndpoints(this RouteGroupBuilder group)
        {
            group.MapServicesFeaturedItemEndpoint();

            return group;
        }
    }
}
