using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ServiceEndpoints
{
    public static class ServiceDependencyInjection
    {
        public static RouteGroupBuilder MapAllServiceConfiguration(this RouteGroupBuilder group)
        {
            group.MapServiceCategoryEndpoints()
                 .MapServiceCategoryGetAllEndpoints()
                 .MapServiceCategoryGetByIdEndpoint()
                 .MapServiceCategoryUpdateEndpoints()
                 .MapServiceAdEndpoints()
                 .MapServiceAdUpdateEndpoints()
                 .MapServiceGetAllEndpoints()
                 .MapServiceGetByIdEndpoint()
                 .MapServiceAdDeleteEndpoint()
                 .MapGetServicesByStatusEndpoint()
                 .MapPromoteEndpoint()
                 .MapFeatureEndpoint()
                 .MapRefreshEndpoint()
                 .MapBulkActionsEndpoint();
            return group;
        }
    }
}
