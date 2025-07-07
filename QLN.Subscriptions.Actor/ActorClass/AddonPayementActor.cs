using Dapr.Actors.Runtime;
using Dapr.Client;

using QLN.Common.Infrastructure.IService.IAddonService;
using static QLN.Common.DTO_s.AddonDto;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class AddonPaymentActor : Dapr.Actors.Runtime.Actor, IAddonPaymentActor
    {
        private const string StateKey = "addon-payment-data";
        private const string StoreName = "statestore";
        private const string GlobalAddonPaymentDetailsKey = "global-addon-payment-details";
        private const string BackupStateKey = "transaction-data";
        private const string DailyTimerName = "addon-daily-timer";
        private const string SpecificTimerName = "addon-specific-timer";
        private readonly ILogger<AddonPaymentActor> _logger;
        private static readonly TimeSpan DailyCheckTime = new TimeSpan(15, 01, 0);
        private static readonly string TimeZoneId = "India Standard Time";
        private readonly DaprClient _daprClient;

        public AddonPaymentActor(ActorHost host, ILogger<AddonPaymentActor> logger, DaprClient daprClient) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _daprClient = daprClient;
        }
        public class GlobalAddonPaymentDetailsCollection
        {
            public List<AddonPaymentWithCurrencyDto> Details { get; set; } = new();
        }

        public async Task StoreGlobalAddonPaymentDetailsAsync(AddonPaymentWithCurrencyDto details, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _daprClient.GetStateAsync<GlobalAddonPaymentDetailsCollection>(
                    StoreName,
                    GlobalAddonPaymentDetailsKey,
                    cancellationToken: cancellationToken);

                existing ??= new GlobalAddonPaymentDetailsCollection();
                existing.Details.RemoveAll(x =>
                    x.UserId == details.UserId &&
                    x.AddonId == details.AddonId);

                existing.Details.Add(details);

                await _daprClient.SaveStateAsync(StoreName, GlobalAddonPaymentDetailsKey, existing, cancellationToken: cancellationToken);

                _logger.LogInformation("[Global] Stored addon payment detail for user {UserId}, AddonId: {AddonId}",
                    details.UserId, details.AddonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing global addon payment detail");
                throw;
            }
        }

        public async Task<List<AddonPaymentWithCurrencyDto>> GetAllGlobalAddonPaymentDetailsAsync(CancellationToken cancellationToken = default)
        {
            var global = await _daprClient.GetStateAsync<GlobalAddonPaymentDetailsCollection>(
                StoreName,
                GlobalAddonPaymentDetailsKey,
                cancellationToken: cancellationToken);

            return global?.Details ?? new List<AddonPaymentWithCurrencyDto>();
        }


        public async Task<bool> SetDataAsync(AddonPaymentDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[AddonPaymentActor {ActorId}] SetDataAsync called for user {UserId}", Id, data.UserId);

            try
            {
                data.IsExpired = false;
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
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error in SetDataAsync", Id);
                throw;
            }
        }

        public async Task<bool> FastSetDataAsync(AddonPaymentDto data, CancellationToken cancellationToken = default)
        {
            return await SetDataAsync(data, cancellationToken);
        }

        public async Task<AddonPaymentDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("[AddonPaymentActor {ActorId}] GetDataAsync called", Id);

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
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error in GetDataAsync", Id);
                throw;
            }
        }

        private async Task StoreInPrimaryStateAsync(AddonPaymentDto data, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.SetStateAsync(StateKey, data, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                _logger.LogInformation("[AddonPaymentActor {ActorId}] Stored data in primary state key '{StateKey}'", Id, StateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error storing in primary state key '{StateKey}'", Id, StateKey);
                throw;
            }
        }

        private async Task StoreInBackupStateAsync(AddonPaymentDto data, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.SetStateAsync(BackupStateKey, data, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                _logger.LogInformation("[AddonPaymentActor {ActorId}] Stored data in backup state key '{BackupStateKey}'", Id, BackupStateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error storing in backup state key '{BackupStateKey}'", Id, BackupStateKey);
                throw;
            }
        }

        private async Task<AddonPaymentDto?> GetFromPrimaryStateAsync(CancellationToken cancellationToken)
        {
            try
            {
                var conditionalValue = await StateManager.TryGetStateAsync<AddonPaymentDto>(StateKey, cancellationToken);
                if (conditionalValue.HasValue)
                {
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] Retrieved data from primary state key '{StateKey}'", Id, StateKey);
                    return conditionalValue.Value;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error retrieving from primary state key '{StateKey}'", Id, StateKey);
                throw;
            }
        }

        private async Task<AddonPaymentDto?> GetFromBackupStateAsync(CancellationToken cancellationToken)
        {
            try
            {
                var conditionalValue = await StateManager.TryGetStateAsync<AddonPaymentDto>(BackupStateKey, cancellationToken);
                if (conditionalValue.HasValue)
                {
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] Retrieved data from backup state key '{BackupStateKey}'", Id, BackupStateKey);
                    return conditionalValue.Value;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error retrieving from backup state key '{BackupStateKey}'", Id, BackupStateKey);
                throw;
            }
        }

        public async Task<bool> DeleteDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[AddonPaymentActor {ActorId}] DeleteDataAsync called", Id);
                await StateManager.TryRemoveStateAsync(StateKey, cancellationToken);
                await StateManager.TryRemoveStateAsync(BackupStateKey, cancellationToken);
                await StateManager.SaveStateAsync(cancellationToken);
                await CleanupTimersAsync();

                _logger.LogInformation("[AddonPaymentActor {ActorId}] Deleted data from both state keys and cleaned up timers", Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error in DeleteDataAsync", Id);
                throw;
            }
        }

        public async Task<bool> SyncStateKeysAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[AddonPaymentActor {ActorId}] SyncStateKeysAsync called", Id);

                var primaryData = await GetFromPrimaryStateAsync(cancellationToken);
                var backupData = await GetFromBackupStateAsync(cancellationToken);
                if (primaryData != null && backupData == null)
                {
                    await StoreInBackupStateAsync(primaryData, cancellationToken);
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] Synced data from primary to backup state key", Id);
                }
                else if (backupData != null && primaryData == null)
                {
                    await StoreInPrimaryStateAsync(backupData, cancellationToken);
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] Synced data from backup to primary state key", Id);
                }
                else if (primaryData != null && backupData != null)
                {
                    if (primaryData.LastUpdated > backupData.LastUpdated)
                    {
                        await StoreInBackupStateAsync(primaryData, cancellationToken);
                        _logger.LogInformation("[AddonPaymentActor {ActorId}] Synced newer data from primary to backup state key", Id);
                    }
                    else if (backupData.LastUpdated > primaryData.LastUpdated)
                    {
                        await StoreInPrimaryStateAsync(backupData, cancellationToken);
                        _logger.LogInformation("[AddonPaymentActor {ActorId}] Synced newer data from backup to primary state key", Id);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error in SyncStateKeysAsync", Id);
                throw;
            }
        }

        private async Task ScheduleExpiryChecksAsync(AddonPaymentDto paymentData)
        {
            try
            {
                await CleanupTimersAsync();
                await ScheduleDailyExpiryCheckAsync();
                await ScheduleSpecificExpiryCheckIfNeeded(paymentData);

                _logger.LogInformation("[AddonPaymentActor {ActorId}] Scheduled expiry checks for user {UserId}, EndDate: {EndDate}",
                    Id, paymentData.UserId, paymentData.EndDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error scheduling expiry checks", Id);
            }
        }

        private async Task ScheduleDailyExpiryCheckAsync()
        {
            var (nextCheckTime, dueTime) = CalculateNextDailyCheckTime();

            _logger.LogInformation("[AddonPaymentActor {ActorId}] Scheduling daily expiry check for {NextCheck} IST (in {DueTime})",
                Id, nextCheckTime, dueTime);

            await RegisterTimerAsync(
                DailyTimerName,
                nameof(CheckAddonExpiryAsync),
                null,
                dueTime,
                TimeSpan.FromDays(1));
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

        public async Task CheckAddonExpiryAsync()
        {
            try
            {
                _logger.LogInformation("[AddonPaymentActor {ActorId}] Checking addon expiry at {Time} UTC", Id, DateTime.UtcNow);

                var paymentData = await GetDataAsync();
                if (paymentData == null)
                {
                    _logger.LogWarning("[AddonPaymentActor {ActorId}] No payment data found during expiry check", Id);
                    await CleanupTimersAsync();
                    return;
                }
                if (paymentData.IsExpired == true)
                {
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] Payment already marked as expired for user {UserId}",
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
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] Addon still active for user {UserId}. EndDate: {EndDate}, Days remaining: {DaysRemaining}",
                        Id, paymentData.UserId, paymentData.EndDate, daysRemaining);
                    await ScheduleSpecificExpiryCheckIfNeeded(paymentData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error during addon expiry check", Id);
            }
        }

        private async Task HandleSubscriptionExpiryAsync(AddonPaymentDto paymentData)
        {
            _logger.LogInformation("[AddonPaymentActor {ActorId}] Addon expired for user {UserId}. EndDate: {EndDate}",
                Id, paymentData.UserId, paymentData.EndDate);
            paymentData.IsExpired = true;
            paymentData.LastUpdated = DateTime.UtcNow;
            await StoreInPrimaryStateAsync(paymentData, default);
            await StoreInBackupStateAsync(paymentData, default);

            _logger.LogInformation("[AddonPaymentActor {ActorId}] Marked payment as expired for user {UserId}", Id, paymentData.UserId);
            await CleanupTimersAsync();
            _logger.LogInformation("[AddonPaymentActor {ActorId}] Cleaned up timers for expired addon", Id);
        }

        private async Task ScheduleSpecificExpiryCheckIfNeeded(AddonPaymentDto paymentData)
        {
            var timeUntilExpiry = paymentData.EndDate - DateTime.UtcNow;
            var (_, timeUntilNextDailyCheck) = CalculateNextDailyCheckTime();
            if (timeUntilExpiry < timeUntilNextDailyCheck && timeUntilExpiry > TimeSpan.Zero)
            {
                var bufferTime = TimeSpan.FromMinutes(2);
                var specificDueTime = timeUntilExpiry.Add(bufferTime);

                _logger.LogInformation("[AddonPaymentActor {ActorId}] Scheduling specific expiry check in {TimeUntilExpiry} for user {UserId}",
                    Id, specificDueTime, paymentData.UserId);
                await UnregisterTimerSafelyAsync(SpecificTimerName);

                await RegisterTimerAsync(
                    SpecificTimerName,
                    nameof(CheckAddonExpiryAsync),
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
                _logger.LogDebug("[AddonPaymentActor {ActorId}] Successfully unregistered timer {TimerName}", Id, timerName);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[AddonPaymentActor {ActorId}] Timer {TimerName} was not registered or already unregistered", Id, timerName);
            }
        }

        protected override async Task OnActivateAsync()
        {
            _logger.LogInformation("[AddonPaymentActor {ActorId}] Actor activated", Id);

            try
            {
                await SyncStateKeysAsync();

                var paymentData = await GetDataAsync();
                if (paymentData != null)
                {
                    if (paymentData.EndDate > DateTime.UtcNow && paymentData.IsExpired != true)
                    {
                        _logger.LogInformation("[AddonPaymentActor {ActorId}] Reactivating expiry checks for active subscription. EndDate: {EndDate}",
                            Id, paymentData.EndDate);
                        await ScheduleExpiryChecksAsync(paymentData);
                    }
                    else
                    {
                        _logger.LogInformation("[AddonPaymentActor {ActorId}] Subscription already expired or invalid. EndDate: {EndDate}, IsExpired: {IsExpired}",
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
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] No payment data found during activation", Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error during actor activation", Id);
            }

            await base.OnActivateAsync();
        }

        protected override async Task OnDeactivateAsync()
        {
            _logger.LogInformation("[AddonPaymentActor {ActorId}] Actor deactivated", Id);

            await CleanupTimersAsync();

            await base.OnDeactivateAsync();
        }

        public async Task<bool> TriggerExpiryCheckAsync()
        {
            _logger.LogInformation("[AddonPaymentActor {ActorId}] Manual expiry check triggered", Id);
            await CheckAddonExpiryAsync();
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
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] Rescheduled expiry checks", Id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddonPaymentActor {ActorId}] Error rescheduling expiry checks", Id);
                return false;
            }
        }
    }
}