using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints
{
    public static class V2EventEndpointDependency
    {
        public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder group)
        {
            group.MapCreateEventEndpoints();
            return group;
        }
    }
}
