using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;

using QLN.Common.Infrastructure.IService.IAddonService;
using static QLN.Common.DTO_s.AddonDto;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class AddonPaymentActor : Dapr.Actors.Runtime.Actor, IAddonPaymentActor
    {
        private const string StateKey = "addon-payment-data";
        private const string DailyTimerName = "addon-daily-timer";
        private const string SpecificTimerName = "addon-specific-timer";
        private readonly ILogger<AddonPaymentActor> _logger;
        private static readonly TimeSpan DailyCheckTime = new TimeSpan(15, 01, 0);
        private static readonly string TimeZoneId = "India Standard Time";

        public AddonPaymentActor(ActorHost host, ILogger<AddonPaymentActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SetDataAsync(AddonPaymentDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[AddonPaymentActor {ActorId}] SetDataAsync called for user {UserId}", Id, data.UserId);

            // Initialize IsExpired if null
            data.IsExpired = false;
            data.LastUpdated = DateTime.UtcNow;

            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            // Schedule expiry checks only if not expired and has valid end date
            if (!data.IsExpired && data.EndDate > DateTime.UtcNow)
            {
                await ScheduleExpiryChecksAsync(data);
            }
            else if (data.IsExpired)
            {
                // Clean up any existing timers for expired payments
                await CleanupTimersAsync();
            }

            return true;
        }

        public async Task<bool> FastSetDataAsync(AddonPaymentDto data, CancellationToken cancellationToken = default)
        {
            return await SetDataAsync(data, cancellationToken);
        }

        public async Task<AddonPaymentDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("[AddonPaymentActor {ActorId}] GetDataAsync called", Id);

            var conditionalValue = await StateManager.TryGetStateAsync<AddonPaymentDto>(StateKey, cancellationToken);
            return conditionalValue.HasValue ? conditionalValue.Value : null;
        }

        private async Task ScheduleExpiryChecksAsync(AddonPaymentDto paymentData)
        {
            try
            {
                // Clean up existing timers first
                await CleanupTimersAsync();

                // Schedule daily check
                await ScheduleDailyExpiryCheckAsync();

                // Schedule specific expiry check if needed
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

            // Ensure we don't have negative due time
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

                // Skip if already marked as expired
                if (paymentData.IsExpired == true)
                {
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] Payment already marked as expired for user {UserId}",
                        Id, paymentData.UserId);
                    await CleanupTimersAsync();
                    return;
                }

                // Check if subscription has expired
                if (paymentData.EndDate <= DateTime.UtcNow)
                {
                    await HandleSubscriptionExpiryAsync(paymentData);
                }
                else
                {
                    var daysRemaining = (int)(paymentData.EndDate - DateTime.UtcNow).TotalDays;
                    _logger.LogInformation("[AddonPaymentActor {ActorId}] Addon still active for user {UserId}. EndDate: {EndDate}, Days remaining: {DaysRemaining}",
                        Id, paymentData.UserId, paymentData.EndDate, daysRemaining);

                    // Reschedule specific expiry check if needed
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

            // Mark as expired
            paymentData.IsExpired = true;
            paymentData.LastUpdated = DateTime.UtcNow;

            await StateManager.SetStateAsync(StateKey, paymentData);
            await StateManager.SaveStateAsync();

            _logger.LogInformation("[AddonPaymentActor {ActorId}] Marked payment as expired for user {UserId}", Id, paymentData.UserId);

            // Clean up timers
            await CleanupTimersAsync();
            _logger.LogInformation("[AddonPaymentActor {ActorId}] Cleaned up timers for expired addon", Id);
        }

        private async Task ScheduleSpecificExpiryCheckIfNeeded(AddonPaymentDto paymentData)
        {
            var timeUntilExpiry = paymentData.EndDate - DateTime.UtcNow;
            var (_, timeUntilNextDailyCheck) = CalculateNextDailyCheckTime();

            // Schedule specific timer if expiry occurs before next daily check
            if (timeUntilExpiry < timeUntilNextDailyCheck && timeUntilExpiry > TimeSpan.Zero)
            {
                // Add small buffer to ensure we check after expiry
                var bufferTime = TimeSpan.FromMinutes(2);
                var specificDueTime = timeUntilExpiry.Add(bufferTime);

                _logger.LogInformation("[AddonPaymentActor {ActorId}] Scheduling specific expiry check in {TimeUntilExpiry} for user {UserId}",
                    Id, specificDueTime, paymentData.UserId);

                // Unregister existing specific timer first
                await UnregisterTimerSafelyAsync(SpecificTimerName);

                await RegisterTimerAsync(
                    SpecificTimerName,
                    nameof(CheckAddonExpiryAsync),
                    null,
                    specificDueTime,
                    TimeSpan.FromMilliseconds(-1)); // One-time execution
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

                        // If subscription is expired but not marked, handle it
                        if (paymentData.EndDate <= DateTime.UtcNow && paymentData.IsExpired != true)
                        {
                            await HandleSubscriptionExpiryAsync(paymentData);
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