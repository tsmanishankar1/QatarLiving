using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Services
{
    public class ConnectionService
    {
        [JSInvokable]
        public static Task CheckConnection()
        {
            // This method doesn't need to do anything.
            // If it can be called, the SignalR connection is still up.
            return Task.CompletedTask;
        }
    }
}
