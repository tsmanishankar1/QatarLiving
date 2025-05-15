using Dapr.Actors;
using Dapr.Actors.Runtime;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Threading.Tasks;


public class SubscriptionActor : Actor, ISubscriptionActor, IRemindable
{
    private const string SubscriptionStateName = "statestore";

    public SubscriptionActor(ActorHost host) : base(host)
    {
    }

    public async Task SetDataAsync(SubscriptionDto subscription)
    {
        await StateManager.SetStateAsync(SubscriptionStateName, subscription);
    }

    public async Task<SubscriptionDto> GetDataAsync()
    {
        return await StateManager.GetStateAsync<SubscriptionDto>(SubscriptionStateName);
    }

    public async Task ExpireSubscription()
    {
        var data = await GetDataAsync();
        if (data != null)
        {
            data.Status = SubscriptionStatus.Expired;
            await SetDataAsync(data);
        }
    }

    public async Task RegisterReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
    {
        await RegisterReminderAsync(reminderName, null, dueTime, period);
    }

    public async Task UnregisterReminder(string reminderName)
    {
        try
        {
            await UnregisterReminderAsync(reminderName);
        }
        catch (Exception)
        {
            // Log or ignore if reminder does not exist
        }
    }

    public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
    {
        // Reminder logic here (e.g. expire subscription)
        return Task.CompletedTask;
    }
}
