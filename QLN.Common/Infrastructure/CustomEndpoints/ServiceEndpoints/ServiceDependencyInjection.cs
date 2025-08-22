using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints;
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
            group
                 .MapServiceSearch()
                 .MapServiceCategoryEndpoints()
                 //.MapServiceCategoryGetAllEndpoints()
                 //.MapServiceCategoryGetByIdEndpoint()
                 //.MapServiceCategoryUpdateEndpoints()
                 .MapServiceAdEndpoints()
                 .MapServiceAdUpdateEndpoints()
                 .MapServiceGetAllEndpoints()
                 .MapServiceGetByIdEndpoint()
                 .MapServiceGetBySlugEndpoint()
                 .MapServiceAdDeleteEndpoint()
                 .MapGetAllWithPagination()
                 .MapPromoteEndpoint()
                 .MapFeatureEndpoint()
                 .MapRefreshEndpoint()
                 .MapPublishEndpoint()
                 .MapDetailedGetByIdEndpoint()
                 .MapBulkActionsEndpoint()
                 .MapServicesFeaturedItemEndpoint()
                 .MapGetCategoryCount()
                 .MapServiceCountbySubverticalEndpoints();
            return group;
        }
    }
}
