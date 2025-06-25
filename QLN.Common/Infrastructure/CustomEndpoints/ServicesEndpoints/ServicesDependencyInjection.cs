using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.CustomEndpoints.ServicesEndpoints;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class ServicesDependencyInjection
    {
        public static RouteGroupBuilder MapServicesEndpoints(this RouteGroupBuilder group)
        {
            group.MapServicesFeaturedItemEndpoint()
                .MapServiceEndpoints();

            return group;
        }
    }
}
