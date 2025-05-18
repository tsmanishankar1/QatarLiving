using Microsoft.AspNetCore.Routing;

namespace QLN.Common.Infrastructure.CustomEndpoints.SaveSearchEndPoints
{
    public static class SearchEndPointDependency
    {
        public static RouteGroupBuilder MapSearchEndpoints(this RouteGroupBuilder group)
        {
            group.MapSaveSearchEndpoint()
                 .MapGetSavedSearchesEndpoint();

            return group;
        }
    }
}
