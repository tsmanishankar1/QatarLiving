using Dapr.Actors.Runtime;
using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToFeatureService;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PayToFeaturePaymentActor : Dapr.Actors.Runtime.Actor, IPaymentActor
    {
        private const string StateKey = "paytofeature-payment-data";
        private const string BackupStateKey = "transaction-data";
        private const string StoreName = "statestore";
        private const string GlobalPaymentDetailsKey = "paytopublish-payment-details-collection";
        private const string PaymentIdsStateKey = "payment-ids-collection";
        private const string DailyTimerName = "paytofeature-daily-timer";
        private const string SpecificTimerName = "paytofeature-specific-timer";
        private readonly ILogger<PayToFeaturePaymentActor> _logger;
        private static readonly TimeSpan DailyCheckTime = new TimeSpan(00, 00, 0);
        private static readonly string TimeZoneId = "India Standard Time";
        private readonly DaprClient _daprClient;
        public PayToFeaturePaymentActor(ActorHost host, ILogger<PayToFeaturePaymentActor> logger, DaprClient daprClient) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _daprClient = daprClient;
        }
        public class GlobalP2FPaymentDetailsCollection
        {
            public List<UserP2FPaymentDetailsResponseDto> Details { get; set; } = new();
        }
        public async Task StorePaymentDetailsAsync(UserP2FPaymentDetailsResponseDto details, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _daprClient.GetStateAsync<GlobalP2FPaymentDetailsCollection>(
                    StoreName,
                    GlobalPaymentDetailsKey,
                    cancellationToken: cancellationToken);

                existing ??= new GlobalP2FPaymentDetailsCollection();
                existing.Details.RemoveAll(x => x.UserId == details.UserId);

                existing.Details.Add(details);

                await _daprClient.SaveStateAsync(StoreName, GlobalPaymentDetailsKey, existing, cancellationToken: cancellationToken);

                _logger.LogInformation("[Global] Stored payment detail for user {UserId}", details.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing global paytopublish payment detail");
                throw;
            }
        }
        public async Task<List<UserP2FPaymentDetailsResponseDto>> GetAllPaymentDetailsAsync(CancellationToken cancellationToken = default)
        {
            var global = await _daprClient.GetStateAsync<GlobalP2FPaymentDetailsCollection>(
                StoreName,
                GlobalPaymentDetailsKey,
                cancellationToken: cancellationToken);

            return global?.Details ?? new List<UserP2FPaymentDetailsResponseDto>();
        }
        public async Task<bool> SetDataAsync(PayToFeaturePaymentDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PaymentActor {ActorId}] SetDataAsync called for user {UserId}", Id, data.UserId);

            try
            {

                if (data.IsExpired == null)
                {
                    data.IsExpired = false;
                }
                data.LastUpdated = DateTime.UtcNow;


                await StoreInPrimaryStateAsync(data, cancellationToken);
                await StoreInBackupStateAsync(data, cancellationToken);
                if (!data.IsExpired && data.EndDate > DateTime.UtcNow)
                {
                    await ScheduleExpiryChecksAsync(data);
                }
                else if (data.IsExpired)
                {

                    await CleanupTimersAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error in SetDataAsync", Id);
                throw;
            }
        }

        public async Task<bool> FastSetDataAsync(PayToFeaturePaymentDto data, CancellationToken cancellationToken = default)
        {
            return await SetDataAsync(data, cancellationToken);
        }

        public async Task<PayToFeaturePaymentDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] GetDataAsync called", Id);

            try
            {

                var primaryStateValue = await GetFromPrimaryStateAsync(cancellationToken);
                if (primaryStateValue != null)
                {
                    return primaryStateValue;
                }


                var backupStateValue = await GetFromBackupStateAsync(cancellationToken);
                if (backupStateValue != null)
                {

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


        public async Task<bool> AddPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var paymentIds = await GetPaymentIdsAsync(cancellationToken);
                if (!paymentIds.Contains(paymentId))
                {
                    paymentIds.Add(paymentId);
                    await StateManager.SetStateAsync(PaymentIdsStateKey, paymentIds, cancellationToken);
                    await StateManager.SaveStateAsync(cancellationToken);
                    _logger.LogInformation("Added payment ID {PaymentId} to actor {ActorId}", paymentId, Id);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment ID {PaymentId} to actor {ActorId}", paymentId, Id);
                return false;
            }
        }

        public async Task<List<Guid>> GetAllPaymentIdsAsync(CancellationToken cancellationToken = default)
        {
            return await GetPaymentIdsAsync(cancellationToken);
        }

        private async Task<List<Guid>> GetPaymentIdsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await StateManager.TryGetStateAsync<List<Guid>>(PaymentIdsStateKey, cancellationToken);
                return result.HasValue ? result.Value : new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment IDs from actor {ActorId}", Id);
                return new List<Guid>();
            }
        }

        private async Task StoreInPrimaryStateAsync(PayToFeaturePaymentDto data, CancellationToken cancellationToken)
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

        private async Task StoreInBackupStateAsync(PayToFeaturePaymentDto data, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.SetStateAsync(BackupStateKey, data, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                _logger.LogInformation("[PaymentActor {ActorId}] Stored data in backup state key '{BackupStateKey}'", Id, BackupStateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error storing in backup state key '{BackupStateKey}'", Id, BackupStateKey);
                throw;
            }
        }

        private async Task<PayToFeaturePaymentDto?> GetFromPrimaryStateAsync(CancellationToken cancellationToken)
        {
            try
            {
                var conditionalValue = await StateManager.TryGetStateAsync<PayToFeaturePaymentDto>(StateKey, cancellationToken);
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

        private async Task<PayToFeaturePaymentDto?> GetFromBackupStateAsync(CancellationToken cancellationToken)
        {
            try
            {
                var conditionalValue = await StateManager.TryGetStateAsync<PayToFeaturePaymentDto>(BackupStateKey, cancellationToken);
                if (conditionalValue.HasValue)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Retrieved data from backup state key '{BackupStateKey}'", Id, BackupStateKey);
                    return conditionalValue.Value;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error retrieving from backup state key '{BackupStateKey}'", Id, BackupStateKey);
                throw;
            }
        }

        public async Task<bool> DeleteDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] DeleteDataAsync called", Id);


                await StateManager.TryRemoveStateAsync(StateKey, cancellationToken);
                await StateManager.TryRemoveStateAsync(BackupStateKey, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                await CleanupTimersAsync();

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
                if (primaryData != null && backupData == null)
                {
                    await StoreInBackupStateAsync(primaryData, cancellationToken);
                    _logger.LogInformation("[PaymentActor {ActorId}] Synced data from primary to backup state key", Id);
                }
                else if (backupData != null && primaryData == null)
                {
                    await StoreInPrimaryStateAsync(backupData, cancellationToken);
                    _logger.LogInformation("[PaymentActor {ActorId}] Synced data from backup to primary state key", Id);
                }
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

        private async Task ScheduleExpiryChecksAsync(PayToFeaturePaymentDto paymentData)
        {
            try
            {
                await CleanupTimersAsync();
                await ScheduleDailyExpiryCheckAsync();
                await ScheduleSpecificExpiryCheckIfNeeded(paymentData);

                _logger.LogInformation("[PaymentActor {ActorId}] Scheduled expiry checks for user {UserId}, EndDate: {EndDate}",
                    Id, paymentData.UserId, paymentData.EndDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error scheduling expiry checks", Id);
            }
        }

        private async Task ScheduleDailyExpiryCheckAsync()
        {
            var (nextCheckTime, dueTime) = CalculateNextDailyCheckTime();

            _logger.LogInformation("[PaymentActor {ActorId}] Scheduling daily expiry check for {NextCheck} IST (in {DueTime})",
                Id, nextCheckTime, dueTime);

            await RegisterTimerAsync(
                DailyTimerName,
                nameof(CheckPaytoFeatureExpiryAsync),
                null,
                dueTime,
                TimeSpan.FromDays(1)); // Repeat daily
        }

        private (DateTime nextCheckTime, TimeSpan dueTime) CalculateNextDailyCheckTime()
        {
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);

            var todayCheckTime = nowIst.Date.Add(DailyCheckTime);
            var nextCheckTime = nowIst <= todayCheckTime ? todayCheckTime : todayCheckTime.AddDays(1);
            var nextCheckTimeUtc = TimeZoneInfo.ConvertTimeToUtc(nextCheckTime, istTimeZone);
            var dueTime = nextCheckTimeUtc - DateTime.UtcNow;
            if (dueTime <= TimeSpan.Zero)
            {
                nextCheckTime = nextCheckTime.AddDays(1);
                nextCheckTimeUtc = TimeZoneInfo.ConvertTimeToUtc(nextCheckTime, istTimeZone);
                dueTime = nextCheckTimeUtc - DateTime.UtcNow;
            }

            return (nextCheckTime, dueTime);
        }

        public async Task CheckPaytoFeatureExpiryAsync()
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] Checking pay-to-publish expiry at {Time} UTC", Id, DateTime.UtcNow);

                var paymentData = await GetDataAsync();
                if (paymentData == null)
                {
                    _logger.LogWarning("[PaymentActor {ActorId}] No payment data found during expiry check", Id);
                    await CleanupTimersAsync();
                    return;
                }
                if (paymentData.IsExpired == true)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Payment already marked as expired for user {UserId}",
                        Id, paymentData.UserId);
                    await CleanupTimersAsync();
                    return;
                }
                if (paymentData.EndDate <= DateTime.UtcNow)
                {
                    await HandleSubscriptionExpiryAsync(paymentData);
                }
                else
                {
                    var daysRemaining = (int)(paymentData.EndDate - DateTime.UtcNow).TotalDays;
                    _logger.LogInformation("[PaymentActor {ActorId}] Pay-to-publish still active for user {UserId}. EndDate: {EndDate}, Days remaining: {DaysRemaining}",
                        Id, paymentData.UserId, paymentData.EndDate, daysRemaining);
                    await ScheduleSpecificExpiryCheckIfNeeded(paymentData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error during pay-to-publish expiry check", Id);
            }
        }

        private async Task HandleSubscriptionExpiryAsync(PayToFeaturePaymentDto paymentData)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Pay-to-publish expired for user {UserId}. EndDate: {EndDate}",
                Id, paymentData.UserId, paymentData.EndDate);
            paymentData.IsExpired = true;
            paymentData.LastUpdated = DateTime.UtcNow;
            await StoreInPrimaryStateAsync(paymentData, default);
            await StoreInBackupStateAsync(paymentData, default);

            _logger.LogInformation("[PaymentActor {ActorId}] Marked payment as expired for user {UserId}", Id, paymentData.UserId);

            await CleanupTimersAsync();
            _logger.LogInformation("[PaymentActor {ActorId}] Cleaned up timers for expired pay-to-publish", Id);
        }

        private async Task ScheduleSpecificExpiryCheckIfNeeded(PayToFeaturePaymentDto paymentData)
        {
            var timeUntilExpiry = paymentData.EndDate - DateTime.UtcNow;
            var (_, timeUntilNextDailyCheck) = CalculateNextDailyCheckTime();
            if (timeUntilExpiry < timeUntilNextDailyCheck && timeUntilExpiry > TimeSpan.Zero)
            {
                var bufferTime = TimeSpan.FromMinutes(2);
                var specificDueTime = timeUntilExpiry.Add(bufferTime);

                _logger.LogInformation("[PaymentActor {ActorId}] Scheduling specific expiry check in {TimeUntilExpiry} for user {UserId}",
                    Id, specificDueTime, paymentData.UserId);
                await UnregisterTimerSafelyAsync(SpecificTimerName);

                await RegisterTimerAsync(
                    SpecificTimerName,
                    nameof(CheckPaytoFeatureExpiryAsync),
                    null,
                    specificDueTime,
                    TimeSpan.FromMilliseconds(-1));
            }
        }

        private async Task CleanupTimersAsync()
        {
            await UnregisterTimerSafelyAsync(DailyTimerName);
            await UnregisterTimerSafelyAsync(SpecificTimerName);
        }

        private async Task UnregisterTimerSafelyAsync(string timerName)
        {
            try
            {
                await UnregisterTimerAsync(timerName);
                _logger.LogDebug("[PaymentActor {ActorId}] Successfully unregistered timer {TimerName}", Id, timerName);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[PaymentActor {ActorId}] Timer {TimerName} was not registered or already unregistered", Id, timerName);
            }
        }

        protected override async Task OnActivateAsync()
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Actor activated", Id);

            try
            {

                await SyncStateKeysAsync();

                var paymentData = await GetDataAsync();
                if (paymentData != null)
                {
                    if (paymentData.EndDate > DateTime.UtcNow && paymentData.IsExpired != true)
                    {
                        _logger.LogInformation("[PaymentActor {ActorId}] Reactivating expiry checks for active subscription. EndDate: {EndDate}",
                            Id, paymentData.EndDate);
                        await ScheduleExpiryChecksAsync(paymentData);
                    }
                    else
                    {
                        _logger.LogInformation("[PaymentActor {ActorId}] Subscription already expired or invalid. EndDate: {EndDate}, IsExpired: {IsExpired}",
                            Id, paymentData.EndDate, paymentData.IsExpired);
                        if (paymentData.EndDate <= DateTime.UtcNow && paymentData.IsExpired != true)
                        {
                            await HandleSubscriptionExpiryAsync(paymentData);
                        }
                        else
                        {
                            await CleanupTimersAsync();
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] No payment data found during activation", Id);
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

            await CleanupTimersAsync();

            await base.OnDeactivateAsync();
        }

        public async Task<bool> TriggerExpiryCheckAsync()
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Manual expiry check triggered", Id);
            await CheckPaytoFeatureExpiryAsync();
            return true;
        }

        public async Task<(bool IsActive, DateTime? EndDate, int? DaysRemaining)> GetSubscriptionStatusAsync()
        {
            var paymentData = await GetDataAsync();
            if (paymentData == null)
                return (false, null, null);

            var isActive = paymentData.IsExpired != true && paymentData.EndDate > DateTime.UtcNow;
            var daysRemaining = isActive ? (int)(paymentData.EndDate - DateTime.UtcNow).TotalDays : 0;

            return (isActive, paymentData.EndDate, daysRemaining);
        }
        public async Task<bool> RescheduleExpiryChecksAsync()
        {
            try
            {
                var paymentData = await GetDataAsync();
                if (paymentData != null && paymentData.EndDate > DateTime.UtcNow && paymentData.IsExpired != true)
                {
                    await ScheduleExpiryChecksAsync(paymentData);
                    _logger.LogInformation("[PaymentActor {ActorId}] Rescheduled expiry checks", Id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error rescheduling expiry checks", Id);
                return false;
            }
        }
    }
}