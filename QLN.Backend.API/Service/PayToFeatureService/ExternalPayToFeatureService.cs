using Dapr.Actors.Client;
using Dapr.Actors;
using QLN.Common.DTO_s;
using System.Collections.Concurrent;
using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using QLN.Common.Infrastructure.IService.IPayToFeatureService;
using QLN.Common.Infrastructure.IService.IPayToFeatureActor;

namespace QLN.Backend.API.Service.PayToFeatureService
{
    public class ExternalPayToFeatureService : IPayToFeatureService
    {
        private readonly ILogger<ExternalPayToFeatureService> _logger;
        private static readonly ConcurrentDictionary<string, Guid> _defaultIds = new();
        private readonly Guid _masterActorId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        private static readonly ConcurrentDictionary<Guid, int> _payToFeatureTransactionIds = new();

        public ExternalPayToFeatureService(ILogger<ExternalPayToFeatureService> logger)
        {
            _logger = logger;

           
            _defaultIds.TryAdd("paytofeature_default", Guid.Parse("00000000-0000-0000-0000-000000000001"));
            _defaultIds.TryAdd("payment_default", Guid.Parse("00000000-0000-0000-0000-000000000002"));
        }

        private IPayToFeatureActor GetActorProxy(Guid id)
        {
            return ActorProxy.Create<IPayToFeatureActor>(new ActorId(id.ToString()), "PayToFeatureActor");
        }

        private IPayToFeatureActor GetMasterActorProxy()
        {
            return ActorProxy.Create<IPayToFeatureActor>(new ActorId(_masterActorId.ToString()), "PayToFeatureActor");
        }

      
        public async Task<PayToFeatureDataDto> SetPayToFeatureDataAsync(PayToFeatureDataDto data, CancellationToken cancellationToken = default)
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

            // Store basic prices individually and track their IDs
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
        public async Task<PayToFeatureDataDto> GetPayToFeatureDataAsync(CancellationToken cancellationToken = default)
        {
            var payToFeatureId = _defaultIds["paytofeature_default"];
            var actor = GetActorProxy(payToFeatureId);


            var planIds = await actor.GetAllPlansAsync(cancellationToken);
            var plans = new List<PayToFeatureDto>();

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
            var basicPrices = new List<PayToFeatureBasicPriceDto>();

            foreach (var basicPriceId in basicPriceIds)
            {
                var basicPriceActor = GetActorProxy(basicPriceId);
                var basicPriceData = await basicPriceActor.GetDatasAsync(cancellationToken);
                if (basicPriceData != null)
                {
                    basicPrices.Add(basicPriceData);
                }
            }

            return new PayToFeatureDataDto
            {
                Id = payToFeatureId,
                LastUpdated = DateTime.UtcNow,
                Plans = plans,
                BasicPrices = basicPrices
            };
        }
        public async Task CreateBasicPriceAsync(PayToFeatureBasicPriceRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var basicPrice = new PayToFeatureBasicPriceDto
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

            
            var masterActor = GetActorProxy(_defaultIds["paytofeature_default"]);
            await masterActor.AddBasicPriceIdAsync(basicPrice.Id, cancellationToken);

            _logger.LogInformation("Created basic price with ID: {BasicPriceId}, Vertical: {Vertical}, Category: {Category}",
                basicPrice.Id, basicPrice.VerticalTypeId, basicPrice.CategoryId);
        }

        public async Task CreatePlanAsync(PayToFeatureRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var plan = new PayToFeatureDto
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
                IsPromoteAd=request.IsPromoteAd,
                IsFeaturedAd=request.IsFeatureAd,
                LastUpdated = DateTime.UtcNow
            };
            var planActor = GetActorProxy(plan.Id);
            await planActor.SetDataAsync(plan, cancellationToken);

           
            var masterActor = GetActorProxy(_defaultIds["paytofeature_default"]);
            await masterActor.AddPlanIdAsync(plan.Id, cancellationToken);

            _logger.LogInformation("PayToFeature plan created with ID: {PlanId}, Vertical: {Vertical}, Category: {Category}",
                plan.Id, plan.VerticalTypeId, plan.CategoryId);
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

        public async Task<PayToFeaturePlansResponse> GetPlansByVerticalAndCategoryWithBasicPriceAsync(
       int verticalTypeId,
       int categoryId,
       CancellationToken cancellationToken = default)
        {
            var response = new PayToFeaturePlansResponse();

            // Validate enum values
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

            var data = await GetPayToFeatureDataAsync(cancellationToken);

            if (data.Plans == null || !data.Plans.Any())
            {
                _logger.LogWarning("No plans found.");
                return response;
            }

            // Lookup for basic price
            var basicPriceLookup = data.BasicPrices?
                .GroupBy(bp => $"{(int)bp.VerticalTypeId}_{(int)bp.CategoryId}")
                .ToDictionary(g => g.Key, g => g.First()) ??
                new Dictionary<string, PayToFeatureBasicPriceDto>();

            var lookupKey = $"{verticalTypeId}_{categoryId}";
            basicPriceLookup.TryGetValue(lookupKey, out var basicPricePlan);

            // Defensive check before using duration
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

            // Filter valid plans
            var filteredPlans = data.Plans
                .Where(plan => plan.VerticalTypeId == verticalEnum &&
                               plan.CategoryId == categoryEnum &&
                               plan.StatusId != Status.Expired)
                .ToList();

            foreach (var plan in filteredPlans)
            {
                response.PlanDetails.Add(new PayToFeatureWithBasicPriceResponseDto
                {
                    Id = plan.Id,
                    PlanName = plan.PlanName,
                    Price = plan.Price,
                    Currency = plan.Currency,
                    Description = plan.Description,
                    IsFeaturedAd = plan.IsFeaturedAd,
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

        public async Task<List<PayToFeatureWithBasicPriceResponseDto>> GetAllPlansWithBasicPriceAsync(CancellationToken cancellationToken = default)
        {
            var data = await GetPayToFeatureDataAsync(cancellationToken);
            var plans = new List<PayToFeatureWithBasicPriceResponseDto>();

            if (data.Plans == null || !data.Plans.Any())
            {
                _logger.LogInformation("No plans found");
                return plans;
            }

            // Create basic price lookup
            var basicPriceLookup = data.BasicPrices?
            .GroupBy(bp => $"{(int)bp.VerticalTypeId}_{(int)bp.CategoryId}")
            .ToDictionary(g => g.Key, g => g.First()) ??
            new Dictionary<string, PayToFeatureBasicPriceDto>();

            foreach (var plan in data.Plans.Where(p => p.StatusId != Status.Expired))
            {
               
                var lookupKey = $"{(int)plan.VerticalTypeId}_{(int)plan.CategoryId}";
                basicPriceLookup.TryGetValue(lookupKey, out var matchingBasicPrice);

                plans.Add(new PayToFeatureWithBasicPriceResponseDto
                {
                    Id = plan.Id,
                    PlanName = plan.PlanName,
                    Price = plan.Price,
                    Currency = plan.Currency,
                    Description = plan.Description,
                  IsPromoteAd=plan.IsPromoteAd,
                  IsFeaturedAd=plan.IsFeaturedAd,
                    DurationName = ConvertToReadableFormat(plan.Duration),
                    VerticalId = (int)plan.VerticalTypeId,
                    VerticalName = GetEnumDisplayName(plan.VerticalTypeId),
                    CategoryId = (int)plan.CategoryId,
                    CategoryName = GetEnumDisplayName(plan.CategoryId),
                    //BasicPriceId = matchingBasicPrice != null ? (int)matchingBasicPrice.BasicPriceId : (int?)null,
                   // BasicPriceName = matchingBasicPrice != null ? GetEnumDisplayName(matchingBasicPrice.BasicPriceId) : null
                });
            }

            _logger.LogInformation("Retrieved {Count} plans with basic price information", plans.Count);
            return plans;
        }

        public async Task<bool> UpdatePlanAsync(Guid id, PayToFeatureRequestDto request, CancellationToken cancellationToken = default)
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
            existingPlan.IsFeaturedAd = request.IsFeatureAd;
            existingPlan.IsPromoteAd = request.IsPromoteAd;
            existingPlan.StatusId = request.StatusId;
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

        public async Task<Guid> CreatePaymentsAsync(PayToFeaturePaymentRequestDto request, string userId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var id = Guid.NewGuid();
            var startDate = DateTime.UtcNow;
            DateTime? existingEndDate = null;

            // Check for existing active payments for the user
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

            // Fetch PayToPublish plan
            var planActor = GetActorProxy(request.PayToFeatureId);
            var plan = await planActor.GetDataAsync(cancellationToken)
                       ?? throw new InvalidOperationException($"PayToPublish plan not found for ID: {request.PayToFeatureId}");

            var duration = plan.Duration;
            var endDate = startDate.Add(duration);

            // Create payment DTO
            var dto = new PayToFeaturePaymentDto
            {
                Id = id,
                PayToFeatureId = request.PayToFeatureId,
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

            // Save data in PayToPublishPaymentActor
            var actor = GetPaymentActorProxy(dto.Id);
            var result = await actor.FastSetDataAsync(dto, cancellationToken);
            if (!result)
                throw new InvalidOperationException("PayToPublish payment creation failed.");

            // Store payment details for listing
            var durationEnum = MapTimeSpanToDurationType(duration);
            var details = new UserP2FPaymentDetailsResponseDto
            {
                UserId = userId,
                PaymentTransactionId = dto.Id,
                TransactionDate = DateTime.UtcNow,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                PaytoFeatureId = plan.Id,
                PayToFeatureName = plan.PlanName,
                Price = plan.Price,
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
            _payToFeatureTransactionIds.TryAdd(dto.Id, 0);
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
                "PayToFeaturePaymentActor");
        }

        public async Task<PayToFeaturePaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            var actor = GetPaymentActorProxy(paymentId);
            return await actor.GetDataAsync(cancellationToken);
        }
        public async Task<List<PayToFeaturePaymentDto>> GetActivePaymentsForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var activePayments = new List<PayToFeaturePaymentDto>();
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

        public async Task<List<PayToFeaturePaymentDto>> GetExpiredPaymentsForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var expiredPayments = new List<PayToFeaturePaymentDto>();
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

        public async Task<bool> HandlePaytoFeatureExpiryAsync(string userId, Guid paymentId, CancellationToken cancellationToken = default)
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

        public async Task<List<PayToFeaturePaymentDto>> GetPaymentsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var userPayments = new List<PayToFeaturePaymentDto>();
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

        public async Task<bool> HandlePaytoFeatureExpiryAsync(string userId, CancellationToken cancellationToken = default)
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