using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.AddonEndpoint
{
    public static class AddonEndpointDependency
    {
        public static RouteGroupBuilder MapAddonEndpoints(this RouteGroupBuilder group)
        {

            group.MapQuantitiesEndpoints()
                 .MapCurrenciesEndpoints()
               .MapUnitCurrenciesEndpoints();
            return group;
        }
    }
}
