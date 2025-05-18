using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.BannerEndPoints
{
    public static class ClassifiedEndPointDependency
    {
        public static RouteGroupBuilder MapClassifiedsEndpoints(this RouteGroupBuilder group)
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
                .MapDeleteBannerImageEndpoint()
                .MapClassifiedLandingEndpoints();
            return group;
        }
    }
}
