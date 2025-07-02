using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints
{
    public static class V2EventEndpointDependency
    {
        public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapCreateEventEndpoints()
                .MapGetEventEndpoints()
                .MapGetAllEventEndpoints()
                .MapUpdateEventEndpoints()
                .MapEventCategories()
                .MapCreateCategories()
                .MapGetEventCategory()
                .MapGetEventCategories()
                .MapGetAllEventSlot()
                .MapExpiredEvents()
                .MapDeleteEventEndpoints();
            return group;
        }
    }
}
