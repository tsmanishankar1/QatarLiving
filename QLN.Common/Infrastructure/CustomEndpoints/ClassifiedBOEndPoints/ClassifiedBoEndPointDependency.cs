using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ClassifiedBOEndPoints
{
    public static class ClassifiedBoEndPointDependency
    {
        public static RouteGroupBuilder MapClassifiedboEndpoints(this RouteGroupBuilder group)
        {
            group.MapClassifiedBoEndpoints();
            return group;        }
    }
}
