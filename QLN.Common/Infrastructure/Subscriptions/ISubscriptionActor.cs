// This is the corrected interface
using Dapr.Actors;
using QLN.Common.DTO_s;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Subscriptions
{
   public interface ISubscriptionActor : IActor
{
    Task<bool> SetDataAsync(SubscriptionDto data, CancellationToken cancellationToken = default);
    Task<SubscriptionDto?> GetDataAsync(CancellationToken cancellationToken = default);
    Task<bool> ExpireSubscription(CancellationToken cancellationToken = default);
    
    // Add a lighter weight version of SetDataAsync that doesn't call SaveStateAsync
    Task<bool> FastSetDataAsync(SubscriptionDto data, CancellationToken cancellationToken = default);
}
}