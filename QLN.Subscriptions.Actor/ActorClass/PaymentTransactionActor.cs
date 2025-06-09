using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using QLN.Common.Infrastructure.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using QLN.Common.Infrastructure.DbContext;

namespace QLN.Subscriptions.Actor.ActorClass
{
    public class PaymentTransactionActor : Dapr.Actors.Runtime.Actor, IPaymentTransactionActor
    {
        private const string StateKey = "payment-transaction-data";
        private const string TimerName = "subscription-expiry-timer";
        private readonly ILogger<PaymentTransactionActor> _logger;

        // Static service provider - set this during application startup
        public static IServiceProvider ServiceProvider { get; set; }

        public PaymentTransactionActor(ActorHost host, ILogger<PaymentTransactionActor> logger) : base(host)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

               
                var today115Pm = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, 15,15, 0);

               
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

                    
                    await HandleExpiredSubscriptionAsync(paymentData.UserId);
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

        private async Task HandleExpiredSubscriptionAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("[PaymentActor {ActorId}] Handling expired subscription for user {UserId}", Id, userId);

                if (ServiceProvider == null)
                {
                    _logger.LogError("[PaymentActor {ActorId}] ServiceProvider is not set", Id);
                    return;
                }

                using var scope = ServiceProvider.CreateScope();
                // NOTE this wont work as this is only implemented in the backend api at the moment - to be discussed.

                // this should be changed to publish a message to a queue that the backend API service is listening
                // to that updates a user object to generate an expiry on the user object - this service should never
                // change that database

                var userManagementService = scope.ServiceProvider.GetRequiredService<IExternalSubscriptionService>();

                var result = await userManagementService.HandleSubscriptionExpiryAsync(userId, CancellationToken.None);

                if (result)
                {
                    _logger.LogInformation("[PaymentActor {ActorId}] Successfully handled subscription expiry for user {UserId}", Id, userId);

                    var paymentData = await GetDataAsync();
                    if (paymentData != null)
                    {
                        paymentData.LastUpdated = DateTime.UtcNow;
                        await StateManager.SetStateAsync(StateKey, paymentData);
                        await StateManager.SaveStateAsync();
                    }

                    await UnregisterTimerAsync(TimerName);
                    _logger.LogInformation("[PaymentActor {ActorId}] Unregistered expiry timer for expired subscription", Id);
                }
                else
                {
                    _logger.LogError("[PaymentActor {ActorId}] Failed to handle subscription expiry for user {UserId}", Id, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentActor {ActorId}] Error handling expired subscription for user {UserId}", Id, userId);
            }
        }


        protected override async Task OnActivateAsync()
        {
            _logger.LogInformation("[PaymentActor {ActorId}] Actor activated", Id);

          
            var paymentData = await GetDataAsync();
            if (paymentData != null && paymentData.EndDate > DateTime.UtcNow)
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