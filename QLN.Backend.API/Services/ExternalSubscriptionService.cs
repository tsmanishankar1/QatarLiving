using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ExternalSubscriptionService:ISubscriptionService
{
    private readonly DaprClient _daprClient;

    public ExternalSubscriptionService(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    public async Task CreateSubscriptionAsync(SubscriptionDto subscription)
    {
        var actorId = new ActorId(subscription.Id.ToString());
        var actor = ActorProxy.Create<ISubscriptionActor>(actorId, "SubscriptionActor");
        await actor.SetDataAsync(subscription);
    }

    public async Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid subscriptionId)
    {
        var actorId = new ActorId(subscriptionId.ToString());
        var actor = ActorProxy.Create<ISubscriptionActor>(actorId, "SubscriptionActor");
        return await actor.GetDataAsync();
    }

    public async Task UpdateSubscriptionAsync(SubscriptionDto subscription)
    {
        var actorId = new ActorId(subscription.Id.ToString());
        var actor = ActorProxy.Create<ISubscriptionActor>(actorId, "SubscriptionActor");
        await actor.SetDataAsync(subscription);
    }

    public async Task ExpireSubscriptionAsync(Guid subscriptionId)
    {
        var actorId = new ActorId(subscriptionId.ToString());
        var actor = ActorProxy.Create<ISubscriptionActor>(actorId, "SubscriptionActor");
        await actor.ExpireSubscription();
    }

    // Additional logic for querying subscriptions by user can be added if you maintain user-subscription mappings in external storage
}
