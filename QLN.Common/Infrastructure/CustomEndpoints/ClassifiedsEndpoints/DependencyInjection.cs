using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.CustomEndpoints;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class DependencyInjection
    {
        public static RouteGroupBuilder MapClassifiedsEndpoints(this RouteGroupBuilder group)
        {
            group.MapClassifiedEndpoints()
                .MapClassifiedsFeaturedItemEndpoint();

            return group;
        }
    }
}
