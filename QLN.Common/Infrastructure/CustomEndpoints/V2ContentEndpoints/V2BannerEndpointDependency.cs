using Microsoft.AspNetCore.Routing;


namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class V2BannerEndpointDependency
    {
        public static RouteGroupBuilder MapBannerPostEndpoints(this RouteGroupBuilder group)
        {
            group.MapCreateBannerEndpoints()
            .MapUpdateBannerEndpoints()
            .MapDeleteBannerEndpoints()
            .MapGetByidBannerEndpoints()
            .MapBannerTypeEndpoints()
            .MapGetByFilterBannerEndpoints()
            .MapGetByVerticalStatusBannerEndpoints()
            .MapReorderBannerEndpoints();
            return group;

        }
    }
}
