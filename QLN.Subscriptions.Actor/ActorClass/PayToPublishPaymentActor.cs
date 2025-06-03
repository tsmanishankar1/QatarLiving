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
                var today259Pm = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, 16, 39, 0);

                var next259Pm = nowIst <= today259Pm ? today259Pm : today259Pm.AddDays(1);
                var next259PmUtc = TimeZoneInfo.ConvertTimeToUtc(next259Pm, istTimeZone);
                var dueTime = next259PmUtc - DateTime.UtcNow;

                if (dueTime <= TimeSpan.Zero)
                {
                    dueTime = TimeSpan.FromDays(1) + dueTime;
                }

                _logger.LogInformation("[PaymentActor {ActorId}] Scheduling expiry check for {NextCheck} IST (in {DueTime})",
                    Id, next259Pm, dueTime);

                await RegisterTimerAsync(
                    TimerName,
                    nameof(CheckPaytopublishExpiryAsync),
                    null,
                    dueTime,
                    TimeSpan.FromDays(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error scheduling expiry check", Id);
            }
        }

        public async Task CheckPaytopublishExpiryAsync()
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] Checking pay-to-publish expiry at {Time}", Id, DateTime.UtcNow);

                var paymentData = await GetDataAsync();
                if (paymentData == null)
                {
                    _logger.LogWarning("[PaymentActor {ActorId}] No payment data found", Id);
                    return;
                }
                if (paymentData.IsExpired == true)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Payment already marked as expired for user {UserId}",
                        Id, paymentData.UserId);
                    return;
                }

                if (paymentData.EndDate <= DateTime.UtcNow)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Pay-to-publish expired for user {UserId}. EndDate: {EndDate}",
                        Id, paymentData.UserId, paymentData.EndDate);
                    paymentData.IsExpired = true;
                    paymentData.LastUpdated = DateTime.UtcNow;
                    await StateManager.SetStateAsync(StateKey, paymentData);
                    await StateManager.SaveStateAsync();

                    _logger.LogInformation("[PaymentActor {ActorId}] Marked payment as expired for user {UserId}", Id, paymentData.UserId);
                    await HandleExpiredPayToPublishAsync(paymentData.UserId, paymentData.Id);
                    await UnregisterTimerAsync(TimerName);
                    _logger.LogInformation("[PaymentActor {ActorId}] Unregistered expiry timer for expired pay-to-publish", Id);
                }
                else
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Pay-to-publish still active for user {UserId}. EndDate: {EndDate}",
                        Id, paymentData.UserId, paymentData.EndDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error during pay-to-publish expiry check", Id);
            }
        }

        private async Task HandleExpiredPayToPublishAsync(Guid userId, Guid paymentId)
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] Notifying service about expired pay-to-publish for user {UserId}, payment {PaymentId}", Id, userId, paymentId);

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
                    _logger.LogInformation("[PaymentActor {ActorId}] Successfully notified service about pay-to-publish expiry for user {UserId}", Id, userId);
                }
                else
                {
                    _logger.LogWarning("[PaymentActor {ActorId}] Service returned false for pay-to-publish expiry notification for user {UserId}", Id, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error notifying service about expired pay-to-publish for user {UserId}", Id, userId);
            }
        }

        protected override async Task OnActivateAsync()
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Actor activated", Id);

            var paymentData = await GetDataAsync();
            if (paymentData != null && paymentData.EndDate > DateTime.UtcNow && paymentData.IsExpired != true)
            {
                await ScheduleExpiryCheckAsync();
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

            await base.OnDeactivateAsync();
        }
    }
}