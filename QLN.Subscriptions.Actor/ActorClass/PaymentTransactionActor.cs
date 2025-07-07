using Dapr.Actors.Runtime;
using Dapr.Client;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.ISubscriptionService;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PaymentTransactionActor : Dapr.Actors.Runtime.Actor, IPaymentTransactionActor, IRemindable
    {
        private const string StateKey = "payment-transaction-data";

        private const string StateStoreName = "statestore";
        private const string GlobalPaymentDetailsKey = "payment-details-collection";
        private const string PaymentDetailsStateKey = "payment-details-collection";
        private const string TimerName = "subscription-expiry-timer";
        private const string ReminderName = "subscription-expiry-reminder";
        private const string PubSubName = "pubsub";
        private const string ExpiryTopicName = "subscription-expiry";

        private readonly ILogger<PaymentTransactionActor> _logger;
        private readonly DaprClient _daprClient;

        public PaymentTransactionActor(ActorHost host, DaprClient dapr, ILogger<PaymentTransactionActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _daprClient = dapr;
        }
        public class UserPaymentDetailsCollection
        {
            public List<UserPaymentDetailsResponseDto> Details { get; set; } = new();
        }

        public async Task<bool> SetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            _logger.LogInformation("[PaymentActor {ActorId}] SetDataAsync called", Id);

            try
            {

                data.LastUpdated = DateTime.UtcNow;

                await StoreTransactionDataAsync(data, cancellationToken);
                await ScheduleExpiryCheckAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error in SetDataAsync", Id);
                throw;
            }
        }

        public async Task<bool> FastSetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default)
        {
            return await SetDataAsync(data, cancellationToken);
        }



        public async Task<bool> StorePaymentDetailsAsync(UserPaymentDetailsResponseDto paymentDetails, CancellationToken cancellationToken = default)
        {
            var existing = await _daprClient.GetStateAsync<UserPaymentDetailsCollection>(
                StateStoreName, GlobalPaymentDetailsKey, cancellationToken: cancellationToken);

            existing ??= new UserPaymentDetailsCollection();

            existing.Details.RemoveAll(d => d.UserId == paymentDetails.UserId);
            existing.Details.Add(paymentDetails);

            await _daprClient.SaveStateAsync(StateStoreName, GlobalPaymentDetailsKey, existing, cancellationToken: cancellationToken);
            return true;
        }





        public async Task<UserPaymentDetailsResponseDto?> GetPaymentDetailsAsync(CancellationToken cancellationToken = default)
        {
            var state = await _daprClient.GetStateAsync<UserPaymentDetailsCollection>(
                StateStoreName, GlobalPaymentDetailsKey, cancellationToken: cancellationToken);
            var userIdString = Id.GetGuidId().ToString();

            return state?.Details.FirstOrDefault(d => d.UserId == userIdString);
        }



        public async Task<PaymentTransactionDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] GetDataAsync called", Id);

            try
            {
                var conditionalValue = await StateManager.TryGetStateAsync<PaymentTransactionDto>(StateKey, cancellationToken);
                if (conditionalValue.HasValue)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Retrieved transaction data successfully", Id);
                    return conditionalValue.Value;
                }

                _logger.LogWarning("[PaymentActor {ActorId}] No transaction data found", Id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error in GetDataAsync", Id);
                throw;
            }
        }

        private async Task StoreTransactionDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.SetStateAsync(StateKey, data, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                _logger.LogInformation("[PaymentActor {ActorId}] Stored transaction data", Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error storing transaction data", Id);
                throw;
            }
        }

        public async Task<bool> DeleteDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] DeleteDataAsync called", Id);
                await StateManager.TryRemoveStateAsync(StateKey, cancellationToken);
                await StateManager.TryRemoveStateAsync(PaymentDetailsStateKey, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                await CleanupTimersAndRemindersAsync();

                _logger.LogInformation("[PaymentActor {ActorId}] Deleted all data and cleaned up timers", Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error in DeleteDataAsync", Id);
                throw;
            }
        }

        public async Task<bool> RemoveTransactionDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] RemoveTransactionDataAsync called - removing transaction data but keeping payment details", Id);
                await StateManager.TryRemoveStateAsync(StateKey, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                await CleanupTimersAndRemindersAsync();

                _logger.LogInformation("[PaymentActor {ActorId}] Removed transaction data but preserved payment details", Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error in RemoveTransactionDataAsync", Id);
                throw;
            }
        }

        private async Task ScheduleExpiryCheckAsync()
        {
            try
            {
                var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);
                var today115Pm = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, 00, 00, 0);
                var next115Pm = nowIst <= today115Pm ? today115Pm : today115Pm.AddDays(1);
                var next115PmUtc = TimeZoneInfo.ConvertTimeToUtc(next115Pm, istTimeZone);
                var dueTime = next115PmUtc - DateTime.UtcNow;

                if (dueTime <= TimeSpan.Zero)
                {
                    dueTime = TimeSpan.FromDays(1) + dueTime;
                }

                _logger.LogInformation("[PaymentActor {ActorId}] Scheduling expiry check for {NextCheck} IST (in {DueTime})",
                    Id, next115Pm, dueTime);
                await CleanupTimersAndRemindersAsync();
                await RegisterTimerAsync(
                    TimerName,
                    nameof(CheckSubscriptionExpiryAsync),
                    null,
                    dueTime,
                    TimeSpan.FromDays(1));
                var reminderData = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
                {
                    ScheduledTime = next115PmUtc,
                    Purpose = "SubscriptionExpiryCheck"
                });

                await RegisterReminderAsync(
                    ReminderName,
                    reminderData,
                    dueTime,
                    TimeSpan.FromDays(1));

                _logger.LogInformation("[PaymentActor {ActorId}] Registered both timer and reminder for expiry checks", Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error scheduling expiry check", Id);
            }
        }

        public async Task CheckSubscriptionExpiryAsync()
        {
            await PerformExpiryCheckAsync("Timer");
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Reminder '{ReminderName}' triggered", Id, reminderName);

            if (reminderName == ReminderName)
            {
                await PerformExpiryCheckAsync("Reminder");
            }
        }

        private async Task PerformExpiryCheckAsync(string triggerSource)
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] Checking subscription expiry at {Time} (triggered by {Source})",
                    Id, DateTime.UtcNow, triggerSource);

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

                    var published = await PublishSubscriptionExpiryWithRetryAsync(paymentData);

                    if (published)
                    {
                        paymentData.IsExpired = true;
                        paymentData.LastUpdated = DateTime.UtcNow;

                        await StoreTransactionDataAsync(paymentData, default);
                        await RemoveTransactionDataAsync();

                        _logger.LogInformation("[PaymentActor {ActorId}] Marked subscription as expired, removed transaction data but kept payment details for user {UserId}",
                            Id, paymentData.UserId);
                    }
                    else
                    {
                        _logger.LogWarning("[PaymentActor {ActorId}] Failed to publish expiry message after retries. Will retry on next execution.", Id);
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
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error during subscription expiry check (triggered by {Source})", Id, triggerSource);
            }
        }

        private async Task CleanupTimersAndRemindersAsync()
        {
            try
            {
                await UnregisterTimerAsync(TimerName);
                _logger.LogInformation("[PaymentActor {ActorId}] Unregistered timer '{TimerName}'", Id, TimerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PaymentActor {ActorId}] Error unregistering timer '{TimerName}'", Id, TimerName);
            }

            try
            {
                await UnregisterReminderAsync(ReminderName);
                _logger.LogInformation("[PaymentActor {ActorId}] Unregistered reminder '{ReminderName}'", Id, ReminderName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PaymentActor {ActorId}] Error unregistering reminder '{ReminderName}'", Id, ReminderName);
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
                else if (paymentData != null && paymentData.EndDate <= DateTime.UtcNow)
                {
                    await CleanupTimersAndRemindersAsync();
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
                _logger.LogInformation("[PaymentActor {ActorId}] Unregistered timer on deactivation (reminder persists)", Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PaymentActor {ActorId}] Error unregistering timer during deactivation", Id);
            }

            _daprClient?.Dispose();
            await base.OnDeactivateAsync();
        }
    }
}