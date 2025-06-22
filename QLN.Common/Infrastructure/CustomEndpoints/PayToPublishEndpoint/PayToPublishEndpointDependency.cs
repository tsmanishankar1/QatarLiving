using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.CustomEndpoints.PayToPublishEndpoint
{
    public static class PayToPublishEndpointDependency
    {
        public static RouteGroupBuilder MapPayToPublishEndpoints(this RouteGroupBuilder group)
        {
            // PayToPublish endpoints
            group.MapCreatePayToPublishEndpoints()
                 .MapGetAllPayToPublishEndpoints()
                 .MapDeletePayToPublishEndpoints()
                 .MapGetPayToPublishEndpoints()
                 .MapUpatePayToPublishEndpoints()
                 .MapGetPayToPublishPaymentsByUserEndpoint()
                 .MapGetPayToPublishPaymentsByUserIdEndpoint()
                 .MapCreateBasicPriceEndpoints();

            return group;
        }
    }
}