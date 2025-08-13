using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ServiceBOEndpoint
{
    public static class ServiceBoEndpointDependency
    {

        public static RouteGroupBuilder MapAllServiceBoConfiguration(this RouteGroupBuilder group)
        {
            group.MapServiceAdGetAllEndpoints()
                .MapServiceAdPaymentSummaryEndpoints()
                .MapServiceP2PAdGetAllEndpoints()
                .MapServiceSubscriptionAdGetAllEndpoints()
                .MapGetCompaniesByVertical();
                return group;
        }
    }
}
