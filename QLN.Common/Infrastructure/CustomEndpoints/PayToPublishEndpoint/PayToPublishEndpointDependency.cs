using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;


namespace QLN.Common.Infrastructure.CustomEndpoints.PayToPublishEndpoint
{
    public static class PayToPublishEndpointDependency
    {
        public static RouteGroupBuilder MapPayToPublishEndpoints(this RouteGroupBuilder group)
        {
         ;
            group.MapCreatePayToPublishEndpoints()
                  .MapGetAllPayToPublishEndpoints()
                  .MapDeletePayToPublishEndpoints()
                  .MapGetPayToPublishEndpoints()
                  .MapUpatePayToPublishEndpoints();

                   return group;
        }
    }
}
