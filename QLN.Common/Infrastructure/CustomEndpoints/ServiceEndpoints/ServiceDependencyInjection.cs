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
            group
                 .MapServiceSearch()
                 .MapServiceCategoryEndpoints()
                 .MapServiceCategoryGetAllEndpoints()
                
                 
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
                 .MapP2PromoteEndpoint()
                 .MapP2FeatureEndpoint()
                 .MapP2PublishEndpoint()
                 .MapServiceCountbySubverticalEndpoints();
            return group;
        }
    }
}
