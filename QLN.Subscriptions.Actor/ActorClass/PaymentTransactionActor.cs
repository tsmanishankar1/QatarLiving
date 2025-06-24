using Dapr.Actors;
using Dapr.Actors.Runtime;
using Dapr.Client;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.ISubscriptionService;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PaymentTransactionActor : Dapr.Actors.Runtime.Actor, IPaymentTransactionActor, IRemindable
    {
        private const string StateKey = "payment-transaction-data";
        private const string BackupStateKey = "transaction-data";
        private const string TimerName = "subscription-expiry-timer";
        private const string ReminderName = "subscription-expiry-reminder";
        private const string PubSubName = "pubsub";
        private const string ExpiryTopicName = "subscription-expiry";

        private readonly ILogger<PaymentTransactionActor> _logger;
        private readonly DaprClient _daprClient;

        public PaymentTransactionActor(ActorHost host, ILogger<PaymentTransactionActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create DaprClient directly
            var daprClientBuilder = new DaprClientBuilder();
            _daprClient = daprClientBuilder.Build();
        }

        public async Task<bool> SetDataAsync(PaymentTransactionDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PaymentActor {ActorId}] SetDataAsync called", Id);

            try
            {
                // Update timestamp
                data.LastUpdated = DateTime.UtcNow;

                // Store in both primary and backup state keys
                await StoreInPrimaryStateAsync(data, cancellationToken);
                await StoreInBackupStateAsync(data, cancellationToken);

                // Schedule both timer and reminder when payment data is set
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

        public async Task<PaymentTransactionDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] GetDataAsync called", Id);

            try
            {
                // Try to get from primary state first
                var primaryStateValue = await GetFromPrimaryStateAsync(cancellationToken);
                if (primaryStateValue != null)
                {
                    return primaryStateValue;
                }

                // If not found in primary state, try backup state
                var backupStateValue = await GetFromBackupStateAsync(cancellationToken);
                if (backupStateValue != null)
                {
                    // If found in backup state, also update primary state for consistency
                    await StoreInPrimaryStateAsync(backupStateValue, cancellationToken);
                    return backupStateValue;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error in GetDataAsync", Id);
                throw;
            }
        }

        private async Task StoreInPrimaryStateAsync(PaymentTransactionDto data, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.SetStateAsync(StateKey, data, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                _logger.LogInformation("[PaymentActor {ActorId}] Stored data in primary state key '{StateKey}'", Id, StateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error storing in primary state key '{StateKey}'", Id, StateKey);
                throw;
            }
        }

        private async Task StoreInBackupStateAsync(PaymentTransactionDto data, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.SetStateAsync(BackupStateKey, data, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                _logger.LogInformation("[PaymentActor {ActorId}] Stored data in backup state key '{StateKey}'", Id, BackupStateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error storing in backup state key '{StateKey}'", Id, BackupStateKey);
                throw;
            }
        }

        private async Task<PaymentTransactionDto?> GetFromPrimaryStateAsync(CancellationToken cancellationToken)
        {
            try
            {
                var conditionalValue = await StateManager.TryGetStateAsync<PaymentTransactionDto>(StateKey, cancellationToken);
                if (conditionalValue.HasValue)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Retrieved data from primary state key '{StateKey}'", Id, StateKey);
                    return conditionalValue.Value;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error retrieving from primary state key '{StateKey}'", Id, StateKey);
                throw;
            }
        }

        private async Task<PaymentTransactionDto?> GetFromBackupStateAsync(CancellationToken cancellationToken)
        {
            try
            {
                var conditionalValue = await StateManager.TryGetStateAsync<PaymentTransactionDto>(BackupStateKey, cancellationToken);
                if (conditionalValue.HasValue)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Retrieved data from backup state key '{StateKey}'", Id, BackupStateKey);
                    return conditionalValue.Value;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error retrieving from backup state key '{StateKey}'", Id, BackupStateKey);
                throw;
            }
        }

        public async Task<bool> DeleteDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] DeleteDataAsync called", Id);

                // Delete from both state keys
                await StateManager.TryRemoveStateAsync(StateKey, cancellationToken);
                await StateManager.TryRemoveStateAsync(BackupStateKey, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);

                // Clean up timers and reminders
                await CleanupTimersAndRemindersAsync();

                _logger.LogInformation("[PaymentActor {ActorId}] Deleted data from both state keys and cleaned up timers", Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error in DeleteDataAsync", Id);
                throw;
            }
        }

        public async Task<bool> SyncStateKeysAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] SyncStateKeysAsync called", Id);

                var primaryData = await GetFromPrimaryStateAsync(cancellationToken);
                var backupData = await GetFromBackupStateAsync(cancellationToken);

                // If data exists in primary but not in backup, sync to backup
                if (primaryData != null && backupData == null)
                {
                    await StoreInBackupStateAsync(primaryData, cancellationToken);
                    _logger.LogInformation("[PaymentActor {ActorId}] Synced data from primary to backup state key", Id);
                }
                // If data exists in backup but not in primary, sync to primary
                else if (backupData != null && primaryData == null)
                {
                    await StoreInPrimaryStateAsync(backupData, cancellationToken);
                    _logger.LogInformation("[PaymentActor {ActorId}] Synced data from backup to primary state key", Id);
                }
                // If both exist, use the most recently updated one
                else if (primaryData != null && backupData != null)
                {
                    if (primaryData.LastUpdated > backupData.LastUpdated)
                    {
                        await StoreInBackupStateAsync(primaryData, cancellationToken);
                        _logger.LogInformation("[PaymentActor {ActorId}] Synced newer data from primary to backup state key", Id);
                    }
                    else if (backupData.LastUpdated > primaryData.LastUpdated)
                    {
                        await StoreInPrimaryStateAsync(backupData, cancellationToken);
                        _logger.LogInformation("[PaymentActor {ActorId}] Synced newer data from backup to primary state key", Id);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error in SyncStateKeysAsync", Id);
                throw;
            }
        }

        private async Task ScheduleExpiryCheckAsync()
        {
            try
            {
                var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);
                var today115Pm = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, 11, 06, 0);
                var next115Pm = nowIst <= today115Pm ? today115Pm : today115Pm.AddDays(1);
                var next115PmUtc = TimeZoneInfo.ConvertTimeToUtc(next115Pm, istTimeZone);
                var dueTime = next115PmUtc - DateTime.UtcNow;

                if (dueTime <= TimeSpan.Zero)
                {
                    dueTime = TimeSpan.FromDays(1) + dueTime;
                }

                _logger.LogInformation("[PaymentActor {ActorId}] Scheduling expiry check for {NextCheck} IST (in {DueTime})",
                    Id, next115Pm, dueTime);

                // Unregister existing timers/reminders first
                await CleanupTimersAndRemindersAsync();

                // Register Timer for regular checks
                await RegisterTimerAsync(
                    TimerName,
                    nameof(CheckSubscriptionExpiryAsync),
                    null,
                    dueTime,
                    TimeSpan.FromDays(1));

                // Register Reminder as backup
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
                        // Set IsExpiry to true and update LastUpdated when scheduler finishes
                        paymentData.IsExpired = true;
                        paymentData.LastUpdated = DateTime.UtcNow;

                        await StoreInPrimaryStateAsync(paymentData, default);
                        await StoreInBackupStateAsync(paymentData, default);

                        await CleanupTimersAndRemindersAsync();
                        _logger.LogInformation("[PaymentActor {ActorId}] Marked subscription as expired (IsExpiry=true) and cleaned up timers for user {UserId}",
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
                await SyncStateKeysAsync();

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