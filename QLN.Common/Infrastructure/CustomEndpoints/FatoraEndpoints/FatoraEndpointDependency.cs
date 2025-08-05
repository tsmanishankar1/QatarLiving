using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.CustomEndpoints.FatoraEndpoints
{
    public static class FatoraEndpointDependency
    {
        public static RouteGroupBuilder MapFaturaEndpoints(this RouteGroupBuilder group)
        {
            // PayTh endpoints
            group.MapFatoraSuccessEndpoint()
                .MapFatoraFailureEndpoint();

            return group;
        }
    }
}