using Dapr.Actors;
using Dapr.Actors.Runtime;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Subscriptions
{
    public interface ISubscriptionActor : IActor
    {
        Task SetDataAsync(SubscriptionDto subscription);
        Task<SubscriptionDto> GetDataAsync();
        Task ExpireSubscription();
        Task RegisterReminder(string reminderName, TimeSpan dueTime, TimeSpan period);
        Task UnregisterReminder(string reminderName);
    }

}
