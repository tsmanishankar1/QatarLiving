using Dapr.Actors;
using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PaymentTransactionActor : Dapr.Actors.Runtime.Actor, IPaymentTransactionActor
    {
        private const string StateKey = "payment-transaction-data";
        private const string TimerName = "subscription-expiry-timer";
        private const string PubSubName = "pubsub";
        private const string ExpiryTopicName = "subscription-expiry";

        private readonly ILogger<PaymentTransactionActor> _logger;
        private readonly DaprClient _daprClient;

        public PaymentTransactionActor(ActorHost host, ILogger<PaymentTransactionActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create DaprClient directly instead of using DI
            var daprClientBuilder = new DaprClientBuilder();
            _daprClient = daprClientBuilder.Build();
        }

        public async Task<bool> SetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PaymentActor {ActorId}] SetDataAsync called", Id);

            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            // Schedule the expiry check when payment data is set
            await ScheduleExpiryCheckAsync();

            return true;
        }

        public async Task<bool> FastSetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default)
        {
            return await SetDataAsync(data, cancellationToken);
        }

        public async Task<PaymentTransactionDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] GetDataAsync called", Id);

            var conditionalValue = await StateManager.TryGetStateAsync<PaymentTransactionDto>(StateKey, cancellationToken);

            if (conditionalValue.HasValue)
            {
                return conditionalValue.Value;
            }

            return null;
        }

        private async Task ScheduleExpiryCheckAsync()
        {
            try
            {
                var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);
                var today115Pm = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, 12, 09, 0);
                var next115Pm = nowIst <= today115Pm ? today115Pm : today115Pm.AddDays(1);
                var next115PmUtc = TimeZoneInfo.ConvertTimeToUtc(next115Pm, istTimeZone);
                var dueTime = next115PmUtc - DateTime.UtcNow;

                if (dueTime <= TimeSpan.Zero)
                {
                    dueTime = TimeSpan.FromDays(1) + dueTime;
                }

                _logger.LogInformation("[PaymentActor {ActorId}] Scheduling expiry check for {NextCheck} IST (in {DueTime})",
                    Id, next115Pm, dueTime);

                await RegisterTimerAsync(
                    TimerName,
                    nameof(CheckSubscriptionExpiryAsync),
                    null,
                    dueTime,
                    TimeSpan.FromDays(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error scheduling expiry check", Id);
            }
        }

        public async Task CheckSubscriptionExpiryAsync()
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] Checking subscription expiry at {Time}", Id, DateTime.UtcNow);

                var paymentData = await GetDataAsync();
                if (paymentData == null)
                {
                    _logger.LogWarning("[PaymentActor {ActorId}] No payment data found", Id);
                    return;
                }

                if (paymentData.EndDate <= DateTime.UtcNow)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Subscription expired for user {UserId}. EndDate: {EndDate}",
                        Id, paymentData.UserId, paymentData.EndDate);

                    // Publish subscription expiry message with retry logic
                    var published = await PublishSubscriptionExpiryWithRetryAsync(paymentData);

                    if (published)
                    {
                        // Update state to mark as processed only if publish succeeded
                        paymentData.LastUpdated = DateTime.UtcNow;
                        await StateManager.SetStateAsync(StateKey, paymentData);
                        await StateManager.SaveStateAsync();

                        // Unregister timer as subscription has expired
                        await UnregisterTimerAsync(TimerName);
                        _logger.LogInformation("[PaymentActor {ActorId}] Unregistered expiry timer for expired subscription", Id);
                    }
                    else
                    {
                        _logger.LogWarning("[PaymentActor {ActorId}] Failed to publish expiry message after retries. Will retry on next timer execution.", Id);
                    }
                }
                else
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Subscription still active for user {UserId}. EndDate: {EndDate}",
                        Id, paymentData.UserId, paymentData.EndDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error during subscription expiry check", Id);
            }
        }

        private async Task<bool> PublishSubscriptionExpiryWithRetryAsync(PaymentTransactionDto paymentData)
        {
            const int maxRetries = 3;
            const int baseDelayMs = 1000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await PublishSubscriptionExpiryAsync(paymentData);
                    _logger.LogInformation("[PaymentActor {ActorId}] Successfully published subscription expiry message for user {UserId} on attempt {Attempt}",
                        Id, paymentData.UserId, attempt);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PaymentActor {ActorId}] Failed to publish subscription expiry message for user {UserId} on attempt {Attempt}/{MaxRetries}",
                        Id, paymentData.UserId, attempt, maxRetries);

                    if (attempt < maxRetries)
                    {
                        var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt - 1));
                        await Task.Delay(delay);
                    }
                }
            }

            return false;
        }

        private async Task PublishSubscriptionExpiryAsync(PaymentTransactionDto paymentData)
        {
            try
            {
                // First, check if pubsub component is available
                var healthResponse = await _daprClient.CheckHealthAsync();
                //if (!healthResponse.IsHealthy)
                //{
                //    throw new InvalidOperationException("Dapr sidecar is not healthy");
                //}

                var expiryMessage = new SubscriptionExpiryMessage
                {
                    UserId = paymentData.UserId,
                    SubscriptionId = paymentData.SubscriptionId,
                    PaymentTransactionId = paymentData.Id,
                    ExpiryDate = paymentData.EndDate,
                    ProcessedAt = DateTime.UtcNow
                };

                await _daprClient.PublishEventAsync(PubSubName, ExpiryTopicName, expiryMessage);

                _logger.LogInformation("[PaymentActor {ActorId}] Published subscription expiry message for user {UserId}",
                    Id, paymentData.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error publishing subscription expiry message for user {UserId}. Error: {ErrorMessage}",
                    Id, paymentData.UserId, ex.Message);
                throw;
            }
        }

        protected override async Task OnActivateAsync()
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Actor activated", Id);

            try
            {
                var paymentData = await GetDataAsync();
                if (paymentData != null && paymentData.EndDate > DateTime.UtcNow)
                {
                    await ScheduleExpiryCheckAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error during actor activation", Id);
            }

            await base.OnActivateAsync();
        }

        protected override async Task OnDeactivateAsync()
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Actor deactivated", Id);

            try
            {
                await UnregisterTimerAsync(TimerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PaymentActor {ActorId}] Error unregistering timer during deactivation", Id);
            }

            // Dispose DaprClient
            _daprClient?.Dispose();
            await base.OnDeactivateAsync();
        }
    }
}