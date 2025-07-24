using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Notification.MS.CustomEndpoints.NotificationEndpoints
{
    public static class NotificationEndpointDependency
    {
        public static RouteGroupBuilder MapNotificationEndpoints(this RouteGroupBuilder group)
        {
            group.MapNotificationSubscriber();
            return group;
        }
    }
}
