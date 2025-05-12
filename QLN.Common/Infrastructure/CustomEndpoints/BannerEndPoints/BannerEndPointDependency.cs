using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.BannerEndPoints
{
    public static class BannerEndPointDependency
    {
        public static RouteGroupBuilder MapBannerEndpoints(this RouteGroupBuilder group)
        {
            group.MapCreateBannerEndPoints()
                .MapUpdateBannerEndPoints()
                .MapGetByIdBannerEndPoints()
                .MapGetAllBannerEndPoints()
                .MapDeleteByIdBannerEndPoints()
                .MapUploadImageEndPoints()
                .MapGetImageByIdEndPoints()
                .MapGetAllImageEndPoints()
                .MapUpdateBannerImageEndpoints()
                .MapDeleteBannerImageEndpoint();
            return group;
        }
    }
}
