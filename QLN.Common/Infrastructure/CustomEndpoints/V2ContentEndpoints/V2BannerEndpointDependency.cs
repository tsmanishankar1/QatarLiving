using Microsoft.AspNetCore.Routing;


namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2BannerEndpointDependency
    {
        public static RouteGroupBuilder MapBannerPostEndpoints(this RouteGroupBuilder group)
        {
            group.MapCreateBannerTypeEndpoints()
            .MapBannerLocationEndpoints()
            .MapCreateBannerEndpoints()
            .MapUpdateBannerEndpoints()
            .MapDeleteBannerEndpoints();
            return group;

        }
    }
}
