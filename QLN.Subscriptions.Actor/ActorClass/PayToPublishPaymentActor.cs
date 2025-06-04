using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToPublishService;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PayToPublishPaymentActor : Dapr.Actors.Runtime.Actor, IPaymentActor
    {
        private const string StateKey = "paytopublish-payment-data";
        private const string TimerName = "paytopublish-expiry-timer";
        private readonly ILogger<PayToPublishPaymentActor> _logger;
        public static IServiceProvider ServiceProvider { get; set; }

        // Configuration for check time (make this configurable)
        private static readonly TimeSpan CheckTime = new TimeSpan(11, 04, 0); // 4:39 PM
        private static readonly string TimeZoneId = "India Standard Time";

        public PayToPublishPaymentActor(ActorHost host, ILogger<PayToPublishPaymentActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SetDataAsync(PaymentDto data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _logger.LogInformation("[PaymentActor {ActorId}] SetDataAsync called", Id);
            if (data.IsExpired == null)
            {
                data.IsExpired = false;
            }
            await StateManager.SetStateAsync(StateKey, data, cancellationToken);
            await StateManager.SaveStateAsync(cancellationToken);

            // Schedule expiry check only if not expired
            if (!data.IsExpired)
            {
                await ScheduleExpiryCheckAsync();
            }

            return true;
        }

        public async Task<bool> FastSetDataAsync(PaymentDto data, CancellationToken cancellationToken = default)
        {
            return await SetDataAsync(data, cancellationToken);
        }

        public async Task<PaymentDto?> GetDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] GetDataAsync called", Id);

            var conditionalValue = await StateManager.TryGetStateAsync<PaymentDto>(StateKey, cancellationToken);
            return conditionalValue.HasValue ? conditionalValue.Value : null;
        }

        private async Task ScheduleExpiryCheckAsync()
        {
            try
            {
                // Unregister existing timer first to avoid duplicates
                await UnregisterTimerSafelyAsync(TimerName);

                var (nextCheckTime, dueTime) = CalculateNextCheckTime();

                _logger.LogInformation("[PaymentActor {ActorId}] Scheduling expiry check for {NextCheck} IST (in {DueTime})",
                    Id, nextCheckTime, dueTime);

                await RegisterTimerAsync(
                    TimerName,
                    nameof(CheckPaytopublishExpiryAsync),
                    null,
                    dueTime,
                    TimeSpan.FromDays(1)); // Check daily
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error scheduling expiry check", Id);
            }
        }

        private (DateTime nextCheckTime, TimeSpan dueTime) CalculateNextCheckTime()
        {
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);

            var todayCheckTime = nowIst.Date.Add(CheckTime);
            var nextCheckTime = nowIst <= todayCheckTime ? todayCheckTime : todayCheckTime.AddDays(1);
            var nextCheckTimeUtc = TimeZoneInfo.ConvertTimeToUtc(nextCheckTime, istTimeZone);
            var dueTime = nextCheckTimeUtc - DateTime.UtcNow;
            if (dueTime <= TimeSpan.Zero)
            {
                dueTime = TimeSpan.FromDays(1) + dueTime;
            }

            return (nextCheckTime, dueTime);
        }

        public async Task CheckPaytopublishExpiryAsync()
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] Checking pay-to-publish expiry at {Time}", Id, DateTime.UtcNow);

                var paymentData = await GetDataAsync();
                if (paymentData == null)
                {
                    _logger.LogWarning("[PaymentActor {ActorId}] No payment data found during expiry check", Id);
                    await UnregisterTimerSafelyAsync(TimerName);
                    return;
                }

                if (paymentData.IsExpired == true)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Payment already marked as expired for user {UserId}",
                        Id, paymentData.UserId);
                    await UnregisterTimerSafelyAsync(TimerName);
                    return;
                }
                if (paymentData.EndDate <= DateTime.UtcNow)
                {
                    await HandleSubscriptionExpiryAsync(paymentData);
                }
                else
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Pay-to-publish still active for user {UserId}. EndDate: {EndDate}, Days remaining: {DaysRemaining}",
                        Id, paymentData.UserId, paymentData.EndDate, (paymentData.EndDate - DateTime.UtcNow).Days);

                    await ScheduleSpecificExpiryCheckIfNeeded(paymentData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error during pay-to-publish expiry check", Id);
            }
        }

        private async Task HandleSubscriptionExpiryAsync(PaymentDto paymentData)
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Pay-to-publish expired for user {UserId}. EndDate: {EndDate}",
                Id, paymentData.UserId, paymentData.EndDate);

            paymentData.IsExpired = true;
            paymentData.LastUpdated = DateTime.UtcNow;

            await StateManager.SetStateAsync(StateKey, paymentData);
            await StateManager.SaveStateAsync();

            _logger.LogInformation("[PaymentActor {ActorId}] Marked payment as expired for user {UserId}", Id, paymentData.UserId);

            await HandleExpiredPayToPublishAsync(paymentData.UserId, paymentData.Id);
            await UnregisterTimerSafelyAsync(TimerName);
            _logger.LogInformation("[PaymentActor {ActorId}] Unregistered expiry timer for expired pay-to-publish", Id);
        }

        private async Task ScheduleSpecificExpiryCheckIfNeeded(PaymentDto paymentData)
        {
            var timeUntilExpiry = paymentData.EndDate - DateTime.UtcNow;
            var (_, timeUntilNextDailyCheck) = CalculateNextCheckTime();

            if (timeUntilExpiry < timeUntilNextDailyCheck && timeUntilExpiry > TimeSpan.Zero)
            {
                var specificTimerName = $"{TimerName}-specific";

                _logger.LogInformation("[PaymentActor {ActorId}] Scheduling specific expiry check in {TimeUntilExpiry} for user {UserId}",
                    Id, timeUntilExpiry, paymentData.UserId);

                await RegisterTimerAsync(
                    specificTimerName,
                    nameof(CheckPaytopublishExpiryAsync),
                    null,
                    timeUntilExpiry.Add(TimeSpan.FromMinutes(1)), 
                    TimeSpan.FromMilliseconds(-1)); 
            }
        }

        private async Task HandleExpiredPayToPublishAsync(Guid userId, Guid paymentId)
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] Notifying service about expired pay-to-publish for user {UserId}, payment {PaymentId}",
                    Id, userId, paymentId);

                if (ServiceProvider == null)
                {
                    _logger.LogError("[PaymentActor {ActorId}] ServiceProvider is not set", Id);
                    return;
                }

                using var scope = ServiceProvider.CreateScope();
                var payToPublishService = scope.ServiceProvider.GetRequiredService<IPayToPublishService>();

                var result = await payToPublishService.HandlePaytopyblishExpiryAsync(userId, paymentId, CancellationToken.None);

                if (result)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Successfully handled pay-to-publish expiry for user {UserId}", Id, userId);
                }
                else
                {
                    _logger.LogWarning("[PaymentActor {ActorId}] Service returned false for pay-to-publish expiry handling for user {UserId}", Id, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error handling expired pay-to-publish for user {UserId}, payment {PaymentId}", Id, userId, paymentId);
                throw; 
            }
        }

        private async Task UnregisterTimerSafelyAsync(string timerName)
        {
            try
            {
                await UnregisterTimerAsync(timerName);
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
                var paymentData = await GetDataAsync();
                if (paymentData != null && paymentData.EndDate > DateTime.UtcNow && paymentData.IsExpired != true)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Reactivating expiry check for active subscription. EndDate: {EndDate}",
                        Id, paymentData.EndDate);
                    await ScheduleExpiryCheckAsync();
                }
                else if (paymentData != null)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Subscription already expired or null. EndDate: {EndDate}, IsExpired: {IsExpired}",
                        Id, paymentData.EndDate, paymentData.IsExpired);
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

            await UnregisterTimerSafelyAsync(TimerName);
            await UnregisterTimerSafelyAsync($"{TimerName}-specific");

            await base.OnDeactivateAsync();
        }


        public async Task<bool> TriggerExpiryCheckAsync()
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Manual expiry check triggered", Id);
            await CheckPaytopublishExpiryAsync();
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
    }
}