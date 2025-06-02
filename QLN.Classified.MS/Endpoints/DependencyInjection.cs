using QLN.Common.Infrastructure.CustomEndpoints;

namespace QLN.Classified.MS.Endpoints
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
