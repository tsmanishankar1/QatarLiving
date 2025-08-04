using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.CustomEndpoints.D365Endpoints
{
    public static class D365EndpointDependency
    {
        public static RouteGroupBuilder MapD365Endpoints(this RouteGroupBuilder group)
        {
            // PayTh endpoints
            group.MapD365PayEndpoint();

            return group;
        }
    }
}