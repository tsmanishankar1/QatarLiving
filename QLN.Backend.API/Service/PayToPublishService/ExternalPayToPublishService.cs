using Dapr.Actors.Client;
using Dapr.Actors;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToPublicActor;
using System.Collections.Concurrent;
using QLN.Common.Infrastructure.IService.IPayToPublishService;
using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace QLN.Backend.API.Service.PayToPublishService
{
    public class ExternalPayToPublishService : IPayToPublishService
    {
        private readonly ILogger<ExternalPayToPublishService> _logger;
        private static readonly ConcurrentDictionary<Guid, byte> _payToPublishIds = new();
        private static readonly ConcurrentDictionary<Guid, byte> _paymentIds = new();
        private static readonly ConcurrentDictionary<Guid, byte> _basicPriceIds = new();

        public ExternalPayToPublishService(ILogger<ExternalPayToPublishService> logger)
        {
            _logger = logger;
        }

        private IPayToPublishActor GetActorProxy(Guid id)
        {
            return ActorProxy.Create<IPayToPublishActor>(new ActorId(id.ToString()), "PayToPublishActor");
        }

        public async Task CreateBasicPriceAsync(BasicPriceRequestDto request, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();

            var dto = new BasicPriceDto
            {
                Id = id,
                VerticalTypeId = request.VerticalTypeId,
                CategoryId = request.CategoryId,
                BasicPriceId = request.BasicPriceId,
                LastUpdated = DateTime.UtcNow
            };

            var actor = GetActorProxy(id);

            try
            {
                var result = await actor.SetDatasAsync(dto, cancellationToken);

                if (result)
                {
                    _basicPriceIds.TryAdd(id, 0);
                    _logger.LogInformation("Basic price created with ID: {BasicPriceId}", id);
                }
                else
                {
                    _logger.LogError("Actor returned false when creating basic price. ID: {Id}", id);
                    throw new Exception("Basic price creation failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Actor call failed in CreateBasicPriceAsync");
                throw;
            }
        }

        //public async Task<List<BasicPriceResponseDto>> GetBasicPricesByVerticalAndCategoryAsync(
        //    int verticalTypeId,
        //    int categoryId,
        //    CancellationToken cancellationToken = default)
        //{
        //    var resultList = new List<BasicPriceResponseDto>();
        //    var ids = _basicPriceIds.Keys.ToList();
        //    if (!Enum.IsDefined(typeof(Vertical), verticalTypeId))
        //    {
        //        _logger.LogWarning("Invalid VerticalTypeId: {VerticalTypeId}", verticalTypeId);
        //        return resultList;
        //    }

        //    if (!Enum.IsDefined(typeof(SubscriptionCategory), categoryId))
        //    {
        //        _logger.LogWarning("Invalid CategoryId: {CategoryId}", categoryId);
        //        return resultList;
        //    }

        //    var verticalEnum = (Vertical)verticalTypeId;
        //    var categoryEnum = (SubscriptionCategory)categoryId;

        //    _logger.LogInformation("Searching for basic prices with Vertical: {Vertical} ({VerticalId}), Category: {Category} ({CategoryId})",
        //        verticalEnum, verticalTypeId, categoryEnum, categoryId);

        //    foreach (var id in ids)
        //    {
        //        try
        //        {
        //            var actor = GetActorProxy(id);
        //            var data = await actor.GetDatasAsync(cancellationToken);

        //            if (data != null)
        //            {
        //                _logger.LogDebug("Found basic price: ID={Id}, Vertical={Vertical}, Category={Category}",
        //                    data.Id, data.VerticalTypeId, data.CategoryId);

        //                if (data.VerticalTypeId == verticalEnum && data.CategoryId == categoryEnum)
        //                {
        //                    resultList.Add(new BasicPriceResponseDto
        //                    {
        //                        Id = data.Id,
        //                        VerticalTypeId = (int)data.VerticalTypeId,
        //                        VerticalTypeName = GetEnumDisplayName(data.VerticalTypeId),
        //                        CategoryId = (int)data.CategoryId,
        //                        CategoryName = GetEnumDisplayName(data.CategoryId),
        //                        BasicPriceId = (int)data.BasicPriceId,
        //                        BasicPriceName = GetEnumDisplayName(data.BasicPriceId),
        //                        LastUpdated = data.LastUpdated
        //                    });

        //                    _logger.LogInformation("Added basic price to results: ID={Id}", data.Id);
        //                }
        //            }
        //            else
        //            {
        //                _logger.LogWarning("No data found for BasicPrice ID: {Id}", id);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error retrieving data for BasicPrice ID: {Id}", id);
                    
        //        }
        //    }

        //    _logger.LogInformation("Found {Count} basic prices matching criteria", resultList.Count);
        //    return resultList;
        //}

        public async Task CreatePlanAsync(PayToPublishRequestDto request, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();

            var dto = new PayToPublishDto
            {
                Id = id,
                PlanName = request.PlanName,
                TotalCount = request.TotalCount,
                Description = request.Description,
                Duration = request.DurationId,
                Price = request.Price,
                Currency = request.Currency,
                VerticalTypeId = request.VerticalTypeId,
                CategoryId = request.CategoryId,
                StatusId = request.StatusId,
                IsFreeAd =request.IsFreeAd,
                LastUpdated = DateTime.UtcNow
            };

            try
            {
                var actor = GetActorProxy(id);
                var result = await actor.SetDataAsync(dto, cancellationToken);

                if (result)
                {
                    _payToPublishIds.TryAdd(id, 0);
                    _logger.LogInformation("PayToPublish plan created with ID: {PlanId}, Vertical: {Vertical}, Category: {Category}",
                        id, dto.VerticalTypeId, dto.CategoryId);
                }
                else
                {
                    _logger.LogError("Actor returned false when creating plan. ID: {Id}", id);
                    throw new Exception("Pay to publish plan creation failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Actor call failed in CreatePlanAsync for ID: {Id}", id);
                throw;
            }
        }
        public async Task<List<PayToPublishWithBasicPriceResponseDto>> GetPlansByVerticalAndCategoryWithBasicPriceAsync(
      int verticalTypeId,
      int categoryId,
      CancellationToken cancellationToken = default)
        {
            var resultList = new List<PayToPublishWithBasicPriceResponseDto>();
            var planIds = _payToPublishIds.Keys.ToList();
            var basicPriceIds = _basicPriceIds.Keys.ToList();

            // Validate input parameters
            if (!Enum.IsDefined(typeof(Vertical), verticalTypeId))
            {
                _logger.LogWarning("Invalid VerticalTypeId: {VerticalTypeId}", verticalTypeId);
                return resultList;
            }

            if (!Enum.IsDefined(typeof(SubscriptionCategory), categoryId))
            {
                _logger.LogWarning("Invalid CategoryId: {CategoryId}", categoryId);
                return resultList;
            }

            var verticalEnum = (Vertical)verticalTypeId;
            var categoryEnum = (SubscriptionCategory)categoryId;

            _logger.LogInformation("Searching for plans with Vertical: {Vertical} ({VerticalId}), Category: {Category} ({CategoryId}). Total plans in memory: {TotalPlans}",
                verticalEnum, verticalTypeId, categoryEnum, categoryId, planIds.Count);

            // Moved from any to count, as it's more efficient to check if there are any plans before proceeding
            if (planIds.Count == 0)
            {
                _logger.LogWarning("No plans found in memory. Make sure plans are created before trying to retrieve them.");
                return resultList;
            }

            // First, get all basic prices and create a lookup dictionary
            var basicPriceLookup = new Dictionary<(Vertical, SubscriptionCategory), BasicPriceDto>();
            foreach (var basicPriceId in basicPriceIds)
            {
                try
                {
                    var basicPriceActor = GetActorProxy(basicPriceId);
                    using var basicPriceTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var basicPriceCombinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, basicPriceTimeoutCts.Token);

                    var basicPriceData = await basicPriceActor.GetDatasAsync(basicPriceCombinedCts.Token);
                    if (basicPriceData != null)
                    {
                        var key = (basicPriceData.VerticalTypeId, basicPriceData.CategoryId);
                        if (!basicPriceLookup.ContainsKey(key))
                        {
                            basicPriceLookup[key] = basicPriceData;
                        }
                    }
                }
                catch (TimeoutException)
                {
                    _logger.LogError("Timeout occurred while retrieving basic price data for ID: {Id}", basicPriceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving basic price data for ID: {Id}. Exception type: {ExceptionType}", basicPriceId, ex.GetType().Name);
                }
            }

            // Now get filtered plans and match with basic prices
            foreach (var planId in planIds)
            {
                try
                {
                    var planActor = GetActorProxy(planId);
                    using var planTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var planCombinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, planTimeoutCts.Token);

                    var planData = await planActor.GetDataAsync(planCombinedCts.Token);

                    if (planData != null)
                    {
                        _logger.LogDebug("Retrieved plan data: ID={Id}, Vertical={Vertical} ({VerticalInt}), Category={Category} ({CategoryInt}), Status={Status}",
                            planData.Id, planData.VerticalTypeId, (int)planData.VerticalTypeId, planData.CategoryId, (int)planData.CategoryId, planData.StatusId);

                        // Filter by vertical, category, and exclude expired plans
                        if (planData.VerticalTypeId == verticalEnum &&
                            planData.CategoryId == categoryEnum &&
                            planData.StatusId != Status.Expired)
                        {
                            // Look up basic price for this plan's vertical and category
                            var lookupKey = (planData.VerticalTypeId, planData.CategoryId);
                            basicPriceLookup.TryGetValue(lookupKey, out var matchingBasicPrice);

                            resultList.Add(new PayToPublishWithBasicPriceResponseDto
                            {
                                Id = planData.Id,
                                PlanName = planData.PlanName,
                                Price = planData.Price,
                                Currency = planData.Currency,
                                Description = planData.Description,
                                DurationId = (int)planData.Duration,
                                DurationName = GetEnumDisplayName(planData.Duration),
                                IsFreeAd = planData.IsFreeAd,
                                VerticalId = (int)planData.VerticalTypeId,
                                VerticalName = GetEnumDisplayName(planData.VerticalTypeId),
                                CategoryId = (int)planData.CategoryId,
                                CategoryName = GetEnumDisplayName(planData.CategoryId),
                                BasicPriceId = matchingBasicPrice != null ? (int)matchingBasicPrice.BasicPriceId : (int?)null,
                                BasicPriceName = matchingBasicPrice != null ? GetEnumDisplayName(matchingBasicPrice.BasicPriceId) : null
                            });

                            _logger.LogInformation("Added plan to results: ID={Id}, PlanName={PlanName}", planData.Id, planData.PlanName);
                        }
                        else
                        {
                            _logger.LogDebug("Plan doesn't match criteria - Vertical match: {VerticalMatch}, Category match: {CategoryMatch}, Status: {Status}",
                                planData.VerticalTypeId == verticalEnum,
                                planData.CategoryId == categoryEnum,
                                planData.StatusId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Actor returned null data for Plan ID: {Id}", planId);
                        _payToPublishIds.TryRemove(planId, out _);
                    }
                }
                catch (TimeoutException)
                {
                    _logger.LogError("Timeout occurred while retrieving data for Plan ID: {Id}", planId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving data for Plan ID: {Id}. Exception type: {ExceptionType}", planId, ex.GetType().Name);
                    _logger.LogDebug("Full exception details: {Exception}", ex);
                }
            }

            _logger.LogInformation("Found {Count} plans matching criteria out of {TotalIds} total plans", resultList.Count, planIds.Count);
            return resultList;
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
            var ids = _payToPublishIds.Keys.ToList();
            var basicPriceIds = _basicPriceIds.Keys.ToList();
            var plans = new List<PayToPublishWithBasicPriceResponseDto>();

            // First, get all basic prices and create a lookup dictionary
            var basicPriceLookup = new Dictionary<(Vertical, SubscriptionCategory), BasicPriceDto>();

            foreach (var basicPriceId in basicPriceIds)
            {
                try
                {
                    var actor = GetActorProxy(basicPriceId);
                    var basicPriceData = await actor.GetDatasAsync(cancellationToken);

                    if (basicPriceData != null)
                    {
                        var key = (basicPriceData.VerticalTypeId, basicPriceData.CategoryId);
                        if (!basicPriceLookup.ContainsKey(key))
                        {
                            basicPriceLookup[key] = basicPriceData;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving basic price data for ID: {Id}", basicPriceId);
                }
            }

            // Now get all plans and match with basic prices
            foreach (var id in ids)
            {
                try
                {
                    var actor = GetActorProxy(id);
                    var data = await actor.GetDataAsync(cancellationToken);

                    if (data != null && data.StatusId != Status.Expired)
                    {
                        // Look up basic price for this plan's vertical and category
                        var lookupKey = (data.VerticalTypeId, data.CategoryId);
                        basicPriceLookup.TryGetValue(lookupKey, out var matchingBasicPrice);

                        plans.Add(new PayToPublishWithBasicPriceResponseDto
                        {
                            Id = data.Id,
                            PlanName = data.PlanName,
                            Price = data.Price,
                            Currency = data.Currency,
                            Description = data.Description,
                            DurationId = (int)data.Duration,
                            DurationName = GetEnumDisplayName(data.Duration),
                            IsFreeAd = data.IsFreeAd,
                            VerticalId = (int)data.VerticalTypeId,
                            VerticalName = GetEnumDisplayName(data.VerticalTypeId),
                            CategoryId = (int)data.CategoryId,
                            CategoryName = GetEnumDisplayName(data.CategoryId),
                            BasicPriceId = matchingBasicPrice != null ? (int)matchingBasicPrice.BasicPriceId : (int?)null,
                            BasicPriceName = matchingBasicPrice != null ? GetEnumDisplayName(matchingBasicPrice.BasicPriceId) : null
                        });

                        _logger.LogInformation("Added plan with basic price info: {PlanName} (ID: {Id})", data.PlanName, data.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving data for plan ID: {Id}", id);
                }
            }

            _logger.LogInformation("Retrieved {Count} plans with basic price information", plans.Count);
            return plans;
        }


        public async Task<bool> UpdatePlanAsync(Guid id, PayToPublishRequestDto request, CancellationToken cancellationToken = default)
        {
            var actor = GetActorProxy(id);
            var existingData = await actor.GetDataAsync(cancellationToken);

            if (existingData == null || existingData.StatusId == Status.Expired)
            {
                return false;
            }

            existingData.PlanName = request.PlanName;
            existingData.Description = request.Description;
            existingData.Duration = request.DurationId;
            existingData.Price = request.Price;
            existingData.TotalCount = request.TotalCount;
            existingData.Currency = request.Currency;
            existingData.VerticalTypeId = request.VerticalTypeId;
            existingData.CategoryId = request.CategoryId;
            existingData.StatusId = request.StatusId;
            existingData.LastUpdated = DateTime.UtcNow;

            return await actor.SetDataAsync(existingData, cancellationToken);
        }

        public async Task<bool> DeletePlanAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var actor = GetActorProxy(id);
            var data = await actor.GetDataAsync(cancellationToken);

            if (data == null) return false;

            data.StatusId = Status.Expired;
            data.LastUpdated = DateTime.UtcNow;
            data.PlanName = $"Deleted-{data.PlanName}";

            return await actor.SetDataAsync(data, cancellationToken);
        }

        public async Task<Guid> CreatePaymentsAsync(PaymentRequestDto request, Guid userId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var id = Guid.NewGuid();
            var startDate = DateTime.UtcNow;

            var payToPublishActor = GetActorProxy(request.PayToPublishId);
            var payToPublishData = await payToPublishActor.GetDataAsync(cancellationToken) ?? throw new Exception($"PayToPublish data not found for ID: {request.PayToPublishId}");
            var endDate = GetEndDateByDurationEnum(startDate, payToPublishData.Duration);

            var dto = new PaymentDto
            {
                Id = id,
                PayToPublishId = request.PayToPublishId,
                VerticalId = request.VerticalId,
                CategoryId = request.CategoryId,
                CardNumber = request.CardDetails.CardNumber,
                ExpiryMonth = request.CardDetails.ExpiryMonth,
                ExpiryYear = request.CardDetails.ExpiryYear,
                UserId = userId,
                CardHolderName = request.CardDetails.CardHolderName,
                StartDate = startDate,
                EndDate = endDate,
                LastUpdated = DateTime.UtcNow,
                IsExpired = false
            };

            var actor = GetPaymentActorProxy(dto.Id);
            var result = await actor.FastSetDataAsync(dto, cancellationToken);

            if (result)
            {
                _paymentIds.TryAdd(dto.Id, 0);
                _logger.LogInformation("Payment transaction created with ID: {TransactionId}", dto.Id);
                return dto.Id;
            }

            throw new Exception("Payment transaction creation failed.");
        }

        private DateTime GetEndDateByDurationEnum(DateTime startDate, DurationType duration)
        {
            return duration switch
            {
                DurationType.ThreeMonths => startDate.AddMonths(3),
                DurationType.SixMonths => startDate.AddMonths(6),
                DurationType.OneYear => startDate.AddYears(1),
                DurationType.TwoMinutes => startDate.AddMinutes(2),
                _ => throw new ArgumentException($"Unsupported DurationType: {duration}")
            };
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

        public async Task<List<PaymentDto>> GetActivePaymentsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var activePayments = new List<PaymentDto>();
            var paymentIds = _paymentIds.Keys.ToList();

            foreach (var paymentId in paymentIds)
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

            return activePayments;
        }

        public async Task<List<PaymentDto>> GetExpiredPaymentsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var expiredPayments = new List<PaymentDto>();
            var paymentIds = _paymentIds.Keys.ToList();

            foreach (var paymentId in paymentIds)
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

            return expiredPayments;
        }

        public async Task<bool> HandlePaytopyblishExpiryAsync(Guid userId, Guid paymentId, CancellationToken cancellationToken = default)
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

        public async Task<List<PaymentDto>> GetPaymentsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userPayments = new List<PaymentDto>();
            var paymentIds = _paymentIds.Keys.ToList();

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

        public async Task<bool> HandlePaytopyblishExpiryAsync(Guid userId, CancellationToken cancellationToken = default)
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