using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints
{
    public static class ContentNewsDependency
    {
        public static RouteGroupBuilder MapNewsContentEndpoints(this RouteGroupBuilder group)
        {

            group.MapContentNewsEndpoints()
               .MapContentBannerEndpoints();
            return group;
        }
    }
}
