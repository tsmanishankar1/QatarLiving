using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ContentEndpoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.BannerEndPoints
{
    public static class ClassifiedEndPointDependency
    {

        public static RouteGroupBuilder MapContentLandingEndpoints(this RouteGroupBuilder group)
        {
            group.MapContentLandingEndpoint()
                .MapGetPostBySlugEndpoint()
                .MapGetEventBySlugEndpoint();

            return group;
        }
    }
}
