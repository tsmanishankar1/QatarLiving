using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.CustomEndpoints.PayToFeatureEndpoint;

namespace QLN.Common.Infrastructure.CustomEndpoints.PayToPublishEndpoint
{
    public static class FaturaEndpointDependency
    {
        public static RouteGroupBuilder MapFaturaEndpoints(this RouteGroupBuilder group)
        {
            // PayTh endpoints
            group.MapFaturaPaymentEndpoint()
                .MapFaturaSuccessEndpoint()
                .MapFaturaFailureEndpoint();

            return group;
        }
    }
}