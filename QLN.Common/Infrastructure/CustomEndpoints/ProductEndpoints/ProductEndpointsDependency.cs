using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ProductEndpoints
{
    public static class ProductEndpointsDependency
    {
        public static RouteGroupBuilder MapProductsEndpoints(this RouteGroupBuilder group)
        {
            group.MapProductEndpoints();
            return group;

        }
    }
}
