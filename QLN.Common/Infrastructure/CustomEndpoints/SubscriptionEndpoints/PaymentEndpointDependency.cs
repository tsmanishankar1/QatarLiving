using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints
{
    public static class PaymentEndpointDependency
    {
        public static RouteGroupBuilder MapPaymentEndpoints(this RouteGroupBuilder group)
        {
            group.MapProcessPaymentEndpoint()
                .MapProcessPaytoPublishPaymentEndpoint()
                .MapProcessPaytoFeaturePaymentEndpoint()
                  .MapProcessAddonPaymentEndpoint();


            return group;
        }
    }
}
