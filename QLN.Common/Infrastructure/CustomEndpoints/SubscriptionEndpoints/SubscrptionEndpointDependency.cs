using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints
{
    public static class SubscrptionEndpointDependency
    {
        public static RouteGroupBuilder MapSubscriptionEndpoints(this RouteGroupBuilder group)
        {

            group.MapCreateSubscriptionEndpoints()
                 .MapGetdetails()
               .MapGetAllSubscription()
               .MapUpdateSubscription()
               .MapdeleteSubscription()
               .MapGetUserPaymentDetailsEndpoint();



            return group;
        }
    }
}
