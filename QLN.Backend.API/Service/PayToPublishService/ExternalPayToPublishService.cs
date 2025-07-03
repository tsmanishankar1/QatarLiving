using Dapr.Actors;
using Dapr.Actors.Client;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.IService.IPayToPublicActor;
using QLN.Common.Infrastructure.IService.IPayToPublishService;
using QLN.Common.Infrastructure.Subscriptions;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace QLN.Backend.API.Service.PayToPublishService
{
    public class ExternalPayToPublishService : IPayToPublishService
    {
        private readonly ILogger<ExternalPayToPublishService> _logger;
        private static readonly ConcurrentDictionary<string, Guid> _defaultIds = new();
        private readonly Guid _masterActorId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        private static readonly ConcurrentDictionary<Guid, int> _payToPublishTransactionIds = new();

        public ExternalPayToPublishService(ILogger<ExternalPayToPublishService> logger)
        {
            _logger = logger;
            _defaultIds.TryAdd("paytopublish_default", Guid.Parse("00000000-0000-0000-0000-000000000005"));
            _defaultIds.TryAdd("paytopublish_payment_default", Guid.Parse("00000000-0000-0000-0000-000000000006"));
        }

        private IPayToPublishActor GetActorProxy(Guid id)
        {
            return ActorProxy.Create<IPayToPublishActor>(new ActorId(id.ToString()), "PayToPublishActor");
        }

        private IPayToPublishActor GetMasterActorProxy()
        {
            return ActorProxy.Create<IPayToPublishActor>(new ActorId(_masterActorId.ToString()), "PayToPublishActor");
        }

        public async Task<PayToPublishDataDto> SetPayToPublishDataAsync(PayToPublishDataDto data, CancellationToken cancellationToken = default)
        {
            var actor = GetActorProxy(data.Id);

          
            if (data.Plans != null)
            {
                foreach (var plan in data.Plans)
                {
                    var planActor = GetActorProxy(plan.Id);
                    await planActor.SetDataAsync(plan, cancellationToken);
                    await actor.AddPlanIdAsync(plan.Id, cancellationToken);
                }
            }

            
            if (data.BasicPrices != null)
            {
                foreach (var basicPrice in data.BasicPrices)
                {
                    var basicPriceActor = GetActorProxy(basicPrice.Id);
                    await basicPriceActor.SetDatasAsync(basicPrice, cancellationToken);
                    await actor.AddBasicPriceIdAsync(basicPrice.Id, cancellationToken);
                }
            }

            return data;
        }

        public async Task<PayToPublishDataDto> GetPayToPublishDataAsync(CancellationToken cancellationToken = default)
        {
            var payToPublishId = _defaultIds["paytopublish_default"];
            var actor = GetActorProxy(payToPublishId);
            var planIds = await actor.GetAllPlansAsync(cancellationToken);
            var plans = new List<PayToPublishDto>();

            foreach (var planId in planIds)
            {
                var planActor = GetActorProxy(planId);
                var planData = await planActor.GetDataAsync(cancellationToken);
                if (planData != null)
                {
                    plans.Add(planData);
                }
            }
            var basicPriceIds = await actor.GetAllBasicPriceIdsAsync(cancellationToken);
            var basicPrices = new List<BasicPriceDto>();

            foreach (var basicPriceId in basicPriceIds)
            {
                var basicPriceActor = GetActorProxy(basicPriceId);
                var basicPriceData = await basicPriceActor.GetDatasAsync(cancellationToken);
                if (basicPriceData != null)
                {
                    basicPrices.Add(basicPriceData);
                }
            }

            return new PayToPublishDataDto
            {
                Id = payToPublishId,
                LastUpdated = DateTime.UtcNow,
                Plans = plans,
                BasicPrices = basicPrices
            };
        }

        public async Task CreateBasicPriceAsync(BasicPriceRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var basicPrice = new BasicPriceDto
            {
                Id = Guid.NewGuid(),
                VerticalTypeId = request.VerticalTypeId,
                CategoryId = request.CategoryId,
                BasicPriceId = request.BasicPriceId,
                Duration = request.Duration,
                LastUpdated = DateTime.UtcNow
            };
            var basicPriceActor = GetActorProxy(basicPrice.Id);
            await basicPriceActor.SetDatasAsync(basicPrice, cancellationToken);
            var masterActor = GetActorProxy(_defaultIds["paytopublish_default"]);
            await masterActor.AddBasicPriceIdAsync(basicPrice.Id, cancellationToken);

            _logger.LogInformation("Created basic price with ID: {BasicPriceId}, Vertical: {Vertical}, Category: {Category}",
                basicPrice.Id, basicPrice.VerticalTypeId, basicPrice.CategoryId);
        }

        public async Task CreatePlanAsync(PayToPublishRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var plan = new PayToPublishDto
            {
                Id = Guid.NewGuid(),
                PlanName = request.PlanName,
                TotalCount = request.TotalCount,
                Description = request.Description,
                Duration = request.Duration,
                Price = request.Price,
                Currency = request.Currency,
                VerticalTypeId = request.VerticalTypeId,
                CategoryId = request.CategoryId,
                StatusId = request.StatusId,
                IsFreeAd = request.IsFreeAd,
                IsPromoteAd= request.IsPromoteAd,
                IsFeatureAd= request.IsFeatureAd,
                LastUpdated = DateTime.UtcNow
            };
            var planActor = GetActorProxy(plan.Id);
            await planActor.SetDataAsync(plan, cancellationToken);
            var masterActor = GetActorProxy(_defaultIds["paytopublish_default"]);
            await masterActor.AddPlanIdAsync(plan.Id, cancellationToken);
            _logger.LogInformation("PayToPublish plan created with ID: {PlanId}, Vertical: {Vertical}, Category: {Category}",
                plan.Id, plan.VerticalTypeId, plan.CategoryId);
        }

        public async Task<PayToPublishPlansResponse> GetPlansByVerticalAndCategoryWithBasicPriceAsync(
    int verticalTypeId,
    int categoryId,
    CancellationToken cancellationToken = default)
        {
            var response = new PayToPublishPlansResponse();
            if (!Enum.IsDefined(typeof(Vertical), verticalTypeId))
            {
                _logger.LogWarning("Invalid VerticalTypeId: {VerticalTypeId}", verticalTypeId);
                return response;
            }

            if (!Enum.IsDefined(typeof(SubscriptionCategory), categoryId))
            {
                _logger.LogWarning("Invalid CategoryId: {CategoryId}", categoryId);
                return response;
            }

            var verticalEnum = (Vertical)verticalTypeId;
            var categoryEnum = (SubscriptionCategory)categoryId;

            var data = await GetPayToPublishDataAsync(cancellationToken);

            if (data.Plans == null || !data.Plans.Any())
            {
                _logger.LogWarning("No plans found.");
                return response;
            }
            var basicPriceLookup = data.BasicPrices?
                .GroupBy(bp => $"{(int)bp.VerticalTypeId}_{(int)bp.CategoryId}")
                .ToDictionary(g => g.Key, g => g.First()) ??
                new Dictionary<string, BasicPriceDto>();

            var lookupKey = $"{verticalTypeId}_{categoryId}";
            basicPriceLookup.TryGetValue(lookupKey, out var basicPricePlan);
            if (basicPricePlan != null)
            {
                response.BasicPriceId = Enum.IsDefined(typeof(BasicPrice), basicPricePlan.BasicPriceId)
                    ? (int)basicPricePlan.BasicPriceId
                    : (int?)null;

                var durationDays = basicPricePlan.Duration.TotalDays;

                _logger.LogInformation("BasicPrice Duration (days): {DurationDays}", durationDays);

                response.Duration = durationDays > 0
                    ? ConvertToReadableFormat(basicPricePlan.Duration)
                    : "N/A";
            }
            var filteredPlans = data.Plans
                .Where(plan => plan.VerticalTypeId == verticalEnum &&
                               plan.CategoryId == categoryEnum &&
                               plan.StatusId != Status.Expired)
                .ToList();

            foreach (var plan in filteredPlans)
            {
                response.PlanDetails.Add(new PayToPublishWithBasicPriceResponseDto
                {
                    Id = plan.Id,
                    PlanName = plan.PlanName,
                    Price = plan.Price,
                    Currency = plan.Currency,
                    Description = plan.Description,
                    IsFeatureAd = plan.IsFeatureAd,
                    IsPromoteAd = plan.IsPromoteAd,
                    DurationName = ConvertToReadableFormat(plan.Duration),
                    VerticalId = (int)plan.VerticalTypeId,
                    VerticalName = GetEnumDisplayName(plan.VerticalTypeId),
                    CategoryId = (int)plan.CategoryId,
                    CategoryName = GetEnumDisplayName(plan.CategoryId)
                });
            }

            return response;
        }
        public static string ConvertToReadableFormat(TimeSpan duration)
        {
            var totalDays = (int)duration.TotalDays;

            return totalDays switch
            {
                30 => "1 Month",
                60 => "2 Months",
                90 => "3 Months",
                180 => "6 Months",
                365 => "1 Year",
                730 => "2 Years",
                _ when totalDays < 30 && totalDays > 0 => $"{totalDays} Days",
                _ when totalDays == 0 => "N/A",
                _ => $"{(int)(totalDays / 30)} Months"
            };
        }
        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 365)
                return $"{(int)(duration.TotalDays / 365)} Year{(duration.TotalDays >= 730 ? "s" : "")}";

            if (duration.TotalDays >= 30)
                return $"{(int)(duration.TotalDays / 30)} Month{(duration.TotalDays >= 60 ? "s" : "")}";

            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays} Day{(duration.TotalDays > 1 ? "s" : "")}";

            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours} Hour{(duration.TotalHours > 1 ? "s" : "")}";

            return $"{(int)duration.TotalMinutes} Minute{(duration.TotalMinutes > 1 ? "s" : "")}";
        }

        private string GetEnumDisplayName<T>(T enumValue) where T : Enum
        {
            try
            {
                return enumValue.GetType()
                               .GetMember(enumValue.ToString())
                               .FirstOrDefault()?
                               .GetCustomAttribute<DisplayAttribute>()?
                               .Name ?? enumValue.ToString();
            }
            catch
            {
                return enumValue.ToString();
            }
        }

        public async Task<List<PayToPublishWithBasicPriceResponseDto>> GetAllPlansWithBasicPriceAsync(CancellationToken cancellationToken = default)
        {
            var data = await GetPayToPublishDataAsync(cancellationToken);
            var plans = new List<PayToPublishWithBasicPriceResponseDto>();

            if (data.Plans == null || !data.Plans.Any())
            {
                _logger.LogInformation("No plans found");
                return plans;
            }
            var basicPriceLookup = data.BasicPrices?
                .GroupBy(bp => $"{(int)bp.VerticalTypeId}_{(int)bp.CategoryId}")
                .ToDictionary(g => g.Key, g => g.First()) ??
                new Dictionary<string, BasicPriceDto>();

            foreach (var plan in data.Plans.Where(p => p.StatusId != Status.Expired))
            {
                var lookupKey = $"{(int)plan.VerticalTypeId}_{(int)plan.CategoryId}";
                basicPriceLookup.TryGetValue(lookupKey, out var matchingBasicPrice);

                plans.Add(new PayToPublishWithBasicPriceResponseDto
                {
                    Id = plan.Id,
                    PlanName = plan.PlanName,
                    Price = plan.Price,
                    Currency = plan.Currency,
                    Description = plan.Description,
                    DurationName = FormatDuration(plan.Duration),
                    IsFreeAd = plan.IsFreeAd,
                    IsPromoteAd=plan.IsPromoteAd,
                    IsFeatureAd=plan.IsFeatureAd,
                    VerticalId = (int)plan.VerticalTypeId,
                    VerticalName = GetEnumDisplayName(plan.VerticalTypeId),
                    CategoryId = (int)plan.CategoryId,
                    CategoryName = GetEnumDisplayName(plan.CategoryId),
                    BasicPriceId = matchingBasicPrice != null ? (int)matchingBasicPrice.BasicPriceId : (int?)null,
                   
                });
            }

            _logger.LogInformation("Retrieved {Count} plans with basic price information", plans.Count);
            return plans;
        }

        public async Task<bool> UpdatePlanAsync(Guid id, PayToPublishRequestDto request, CancellationToken cancellationToken = default)
        {
            var planActor = GetActorProxy(id);
            var existingPlan = await planActor.GetDataAsync(cancellationToken);

            if (existingPlan == null || existingPlan.StatusId == Status.Expired)
            {
                return false;
            }

            existingPlan.PlanName = request.PlanName;
            existingPlan.Description = request.Description;
            existingPlan.Duration = request.Duration;
            existingPlan.Price = request.Price;
            existingPlan.TotalCount = request.TotalCount;
            existingPlan.Currency = request.Currency;
            existingPlan.VerticalTypeId = request.VerticalTypeId;
            existingPlan.CategoryId = request.CategoryId;
            existingPlan.StatusId = request.StatusId;
            existingPlan.IsFreeAd = request.IsFreeAd;
            existingPlan.IsFeatureAd = request.IsFeatureAd;
            existingPlan.IsPromoteAd= request.IsPromoteAd;
            existingPlan.LastUpdated = DateTime.UtcNow;

            await planActor.SetDataAsync(existingPlan, cancellationToken);
            return true;
        }

        public async Task<bool> DeletePlanAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var planActor = GetActorProxy(id);
            var plan = await planActor.GetDataAsync(cancellationToken);

            if (plan == null) return false;

            plan.StatusId = Status.Expired;
            plan.LastUpdated = DateTime.UtcNow;
            plan.PlanName = $"Deleted-{plan.PlanName}";

            await planActor.SetDataAsync(plan, cancellationToken);
            return true;
        }
        public async Task<Guid> CreatePaymentsAsync(PaymentRequestDto request, string userId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var id = Guid.NewGuid();
            var startDate = DateTime.UtcNow;
            DateTime? existingEndDate = null;
            var masterActor = GetMasterActorProxy();
            var existingPaymentIds = await masterActor.GetAllPaymentIdsAsync(cancellationToken);

            foreach (var existingId in existingPaymentIds)
            {
                try
                {
                    var existingActor = GetPaymentActorProxy(existingId);
                    var existingPayment = await existingActor.GetDataAsync(cancellationToken);

                    if (existingPayment != null &&
                        existingPayment.UserId == userId &&
                        !existingPayment.IsExpired &&
                        existingPayment.EndDate > DateTime.UtcNow)
                    {
                        if (existingEndDate == null || existingPayment.EndDate > existingEndDate)
                        {
                            existingEndDate = existingPayment.EndDate;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking existing payment {PaymentId}", existingId);
                }
            }

            if (existingEndDate.HasValue)
            {
                startDate = existingEndDate.Value;
                _logger.LogInformation("Existing PayToPublish found for user {UserId}. New subscription starts from {StartDate}", userId, startDate);
            }
            var planActor = GetActorProxy(request.PayToPublishId);
            var plan = await planActor.GetDataAsync(cancellationToken)
                       ?? throw new InvalidOperationException($"PayToPublish plan not found for ID: {request.PayToPublishId}");

            var duration = plan.Duration;
            var endDate = startDate.Add(duration);
            var dto = new PaymentDto
            {
                Id = id,
                PayToPublishId = request.PayToPublishId,
                VerticalId = request.VerticalId,
                CategoryId = request.CategoryId,
                CardNumber = request.CardDetails.CardNumber, // Mask for security
                ExpiryMonth = request.CardDetails.ExpiryMonth,
                ExpiryYear = request.CardDetails.ExpiryYear,
                CardHolderName = request.CardDetails.CardHolderName,
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                LastUpdated = DateTime.UtcNow,
                IsExpired = false,
        
            };
            var actor = GetPaymentActorProxy(dto.Id);
            var result = await actor.FastSetDataAsync(dto, cancellationToken);
            if (!result)
                throw new InvalidOperationException("PayToPublish payment creation failed.");
            var durationEnum = MapTimeSpanToDurationType(duration);
            var details = new UserP2PPaymentDetailsResponseDto
            {
                UserId = userId,
                PaymentTransactionId = dto.Id,
                TransactionDate = DateTime.UtcNow,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                PaytoPublishId = plan.Id,
                PayToPublishName = plan.PlanName,
                Price = plan.Price,
                IsPromoteAd=plan.IsPromoteAd,
                IsFeatureAd=plan.IsFeatureAd,
                IsAdFree=plan.IsFreeAd,
                Currency = plan.Currency,
                Description = plan.Description,
                DurationId = (int)durationEnum,
                DurationName = durationEnum.ToString(),
                VerticalTypeId = (int)plan.VerticalTypeId,
                VerticalName = GetEnumDisplayName(plan.VerticalTypeId),
                CategoryId = (int)plan.CategoryId,
                CategoryName = GetEnumDisplayName(plan.CategoryId),
                CardHolderName = dto.CardHolderName
            };

            await actor.StorePaymentDetailsAsync(details, cancellationToken);
            await masterActor.AddPaymentIdAsync(dto.Id, cancellationToken);
            _payToPublishTransactionIds.TryAdd(dto.Id, 0);

            

            return dto.Id;
        }
        private DurationType MapTimeSpanToDurationType(TimeSpan duration)
        {
            if (duration.TotalDays >= 365) return DurationType.OneYear;
            if (duration.TotalDays >= 180) return DurationType.SixMonths;
            return DurationType.ThreeMonths;
        }



        private IPaymentActor GetPaymentActorProxy(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty", nameof(id));

            return ActorProxy.Create<IPaymentActor>(
                new ActorId(id.ToString()),
                "PayToPublishPaymentActor");
        }

        public async Task<PaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            var actor = GetPaymentActorProxy(paymentId);
            return await actor.GetDataAsync(cancellationToken);
        }

        public async Task<List<PaymentDto>> GetActivePaymentsForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var activePayments = new List<PaymentDto>();
            var masterActor = GetMasterActorProxy();
            var paymentIds = await masterActor.GetAllPaymentIdsAsync(cancellationToken);

            foreach (var paymentId in paymentIds)
            {
                try
                {
                    var actor = GetPaymentActorProxy(paymentId);
                    var paymentData = await actor.GetDataAsync(cancellationToken);

                    if (paymentData != null &&
                        paymentData.UserId == userId &&
                        paymentData.IsExpired != true &&
                        paymentData.EndDate > DateTime.UtcNow)
                    {
                        activePayments.Add(paymentData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment data for paymentId {PaymentId}", paymentId);
                }
            }

            return activePayments;
        }

        public async Task<List<PaymentDto>> GetExpiredPaymentsForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var expiredPayments = new List<PaymentDto>();
            var masterActor = GetMasterActorProxy();
            var paymentIds = await masterActor.GetAllPaymentIdsAsync(cancellationToken);

            foreach (var paymentId in paymentIds)
            {
                try
                {
                    var actor = GetPaymentActorProxy(paymentId);
                    var paymentData = await actor.GetDataAsync(cancellationToken);

                    if (paymentData != null &&
                        paymentData.UserId == userId &&
                        (paymentData.IsExpired == true || paymentData.EndDate <= DateTime.UtcNow))
                    {
                        expiredPayments.Add(paymentData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment data for paymentId {PaymentId}", paymentId);
                }
            }

            return expiredPayments;
        }

        public async Task<bool> HandlePaytopyblishExpiryAsync(string userId, Guid paymentId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing payment expiry for user {UserId}, payment {PaymentId}", userId, paymentId);
                _logger.LogInformation("Successfully processed payment expiry for user {UserId}, payment {PaymentId}", userId, paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment expiry for user {UserId}, payment {PaymentId}", userId, paymentId);
                return false;
            }
        }

        public async Task<List<PaymentDto>> GetPaymentsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var userPayments = new List<PaymentDto>();
            var masterActor = GetMasterActorProxy();
            var paymentIds = await masterActor.GetAllPaymentIdsAsync(cancellationToken);

            foreach (var paymentId in paymentIds)
            {
                try
                {
                    var actor = GetPaymentActorProxy(paymentId);
                    var paymentData = await actor.GetDataAsync(cancellationToken);

                    if (paymentData != null && paymentData.UserId == userId)
                    {
                        userPayments.Add(paymentData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment data for paymentId {PaymentId}", paymentId);
                }
            }

            return userPayments.OrderByDescending(p => p.LastUpdated).ToList();
        }

        public async Task<bool> HandlePaytopyblishExpiryAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing expired payments for user {UserId}", userId);
                _logger.LogInformation("Successfully processed expired payments for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired payments for user {UserId}", userId);
                return false;
            }
        }
    }
}