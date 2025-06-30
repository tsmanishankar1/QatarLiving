using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.CustomEndpoints.PayToFeatureEndpoint;

namespace QLN.Common.Infrastructure.CustomEndpoints.PayToPublishEndpoint
{
    public static class PayToFeatureEndpointDependency
    {
        public static RouteGroupBuilder MapPayToFeatureEndpoints(this RouteGroupBuilder group)
        {
            // PayTh endpoints
            group.MapCreatePayToFeatureEndpoints()
                 .MapGetAllPayToFeatureEndpoints()
                 .MapDeletePayToFeatureEndpoints()
                 .MapGetPayToFeatureEndpoints()
                 .MapUpatePayToFeatureEndpoints()
                 .MapGetPayToFeaturePaymentsByUserEndpoint()
                 .MapGetPayToFeaturePaymentsByUserIdEndpoint()
                 .MapPayToFeatureCreateBasicPriceEndpoints();

            return group;
        }
    }
}