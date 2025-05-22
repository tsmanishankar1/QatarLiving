// This is the corrected interface
using Dapr.Actors;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Subscriptions
{
    public interface ISubscriptionActor : IActor
    {
        Task<bool> SetDataAsync(SubscriptionDto data, CancellationToken cancellationToken = default);
        Task<SubscriptionDto?> GetDataAsync(CancellationToken cancellationToken = default);
        Task<bool> FastSetDataAsync(SubscriptionDto data, CancellationToken cancellationToken = default);
    }
}