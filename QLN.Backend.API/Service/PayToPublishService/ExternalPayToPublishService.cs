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

        public async Task<List<BasicPriceResponseDto>> GetBasicPricesByVerticalAndCategoryAsync(
            int verticalTypeId,
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            var resultList = new List<BasicPriceResponseDto>();
            var ids = _basicPriceIds.Keys.ToList();
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

            _logger.LogInformation("Searching for basic prices with Vertical: {Vertical} ({VerticalId}), Category: {Category} ({CategoryId})",
                verticalEnum, verticalTypeId, categoryEnum, categoryId);

            foreach (var id in ids)
            {
                try
                {
                    var actor = GetActorProxy(id);
                    var data = await actor.GetDatasAsync(cancellationToken);

                    if (data != null)
                    {
                        _logger.LogDebug("Found basic price: ID={Id}, Vertical={Vertical}, Category={Category}",
                            data.Id, data.VerticalTypeId, data.CategoryId);

                        if (data.VerticalTypeId == verticalEnum && data.CategoryId == categoryEnum)
                        {
                            resultList.Add(new BasicPriceResponseDto
                            {
                                Id = data.Id,
                                VerticalTypeId = (int)data.VerticalTypeId,
                                VerticalTypeName = GetEnumDisplayName(data.VerticalTypeId),
                                CategoryId = (int)data.CategoryId,
                                CategoryName = GetEnumDisplayName(data.CategoryId),
                                BasicPriceId = (int)data.BasicPriceId,
                                BasicPriceName = GetEnumDisplayName(data.BasicPriceId),
                                LastUpdated = data.LastUpdated
                            });

                            _logger.LogInformation("Added basic price to results: ID={Id}", data.Id);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No data found for BasicPrice ID: {Id}", id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving data for BasicPrice ID: {Id}", id);
                    
                }
            }

            _logger.LogInformation("Found {Count} basic prices matching criteria", resultList.Count);
            return resultList;
        }

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
        public async Task<PayToPublishListResponseDto> GetPlansByVerticalAndCategoryAsync(
            int verticalTypeId,
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            var resultList = new List<PayToPublishResponseDto>();
            var ids = _payToPublishIds.Keys.ToList();

            
            if (!Enum.IsDefined(typeof(Vertical), verticalTypeId))
            {
                _logger.LogWarning("Invalid VerticalTypeId: {VerticalTypeId}", verticalTypeId);
                return new PayToPublishListResponseDto
                {
                    VerticalId = verticalTypeId,
                    VerticalName = "Invalid",
                    CategoryId = categoryId,
                    CategoryName = "Invalid",
                    PayToPublish = new List<PayToPublishResponseDto>()
                };
            }

            if (!Enum.IsDefined(typeof(SubscriptionCategory), categoryId))
            {
                _logger.LogWarning("Invalid CategoryId: {CategoryId}", categoryId);
                return new PayToPublishListResponseDto
                {
                    VerticalId = verticalTypeId,
                    VerticalName = "Invalid",
                    CategoryId = categoryId,
                    CategoryName = "Invalid",
                    PayToPublish = new List<PayToPublishResponseDto>()
                };
            }

            
            var targetVertical = (Vertical)verticalTypeId;
            var targetCategory = (SubscriptionCategory)categoryId;

            _logger.LogInformation("Searching for plans with Vertical: {Vertical} ({VerticalId}), Category: {Category} ({CategoryId}). Total plans in memory: {TotalPlans}",
                targetVertical, verticalTypeId, targetCategory, categoryId, ids.Count);
            if (!ids.Any())
            {
                _logger.LogWarning("No plans found in memory. Make sure plans are created before trying to retrieve them.");
                return new PayToPublishListResponseDto
                {
                    VerticalId = verticalTypeId,
                    VerticalName = GetEnumDisplayName(targetVertical),
                    CategoryId = categoryId,
                    CategoryName = GetEnumDisplayName(targetCategory),
                    PayToPublish = new List<PayToPublishResponseDto>()
                };
            }

            foreach (var id in ids)
            {
                try
                {
                    var actor = GetActorProxy(id);
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    var data = await actor.GetDataAsync(combinedCts.Token);

                    if (data != null)
                    {
                        _logger.LogDebug("Retrieved plan data: ID={Id}, PlanName={PlanName}, Vertical={Vertical} ({VerticalInt}), Category={Category} ({CategoryInt}), Status={Status}",
                            data.Id, data.PlanName, data.VerticalTypeId, (int)data.VerticalTypeId, data.CategoryId, (int)data.CategoryId, data.StatusId);

                        if (data.VerticalTypeId == targetVertical &&
                            data.CategoryId == targetCategory &&
                            data.StatusId != Status.Expired)
                        {
                            resultList.Add(new PayToPublishResponseDto
                            {
                                Id = data.Id,
                                PlanName = data.PlanName,
                                Price = data.Price,
                                Currency = data.Currency,
                                Description = data.Description,
                                DurationId = (int)data.Duration,
                                DurationName = GetEnumDisplayName(data.Duration)
                            });

                            _logger.LogInformation("Added plan to results: {PlanName} (ID: {Id})", data.PlanName, data.Id);
                        }
                        else
                        {
                            _logger.LogDebug("Plan doesn't match criteria - Vertical match: {VerticalMatch}, Category match: {CategoryMatch}, Not expired: {NotExpired}",
                                data.VerticalTypeId == targetVertical,
                                data.CategoryId == targetCategory,
                                data.StatusId != Status.Expired);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Actor returned null data for PayToPublish ID: {Id}", id);
                        _payToPublishIds.TryRemove(id, out _);
                    }
                }
                catch (TimeoutException)
                {
                    _logger.LogError("Timeout occurred while retrieving data for PayToPublish ID: {Id}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving data for PayToPublish ID: {Id}. Exception type: {ExceptionType}", id, ex.GetType().Name);
                    _logger.LogDebug("Full exception details: {Exception}", ex);
                }
            }

            _logger.LogInformation("Found {Count} plans matching criteria out of {TotalIds} total plans", resultList.Count, ids.Count);
            var verticalName = GetEnumDisplayName(targetVertical);
            var categoryName = GetEnumDisplayName(targetCategory);

            return new PayToPublishListResponseDto
            {
                VerticalId = verticalTypeId,
                VerticalName = verticalName,
                CategoryId = categoryId,
                CategoryName = categoryName,
                PayToPublish = resultList
            };
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

        public async Task<List<PayToPublishResponseDto>> GetAllPlansAsync(CancellationToken cancellationToken = default)
        {
            var ids = _payToPublishIds.Keys.ToList();
            var plans = new List<PayToPublishResponseDto>();

            foreach (var id in ids)
            {
                try
                {
                    var actor = GetActorProxy(id);
                    var data = await actor.GetDataAsync(cancellationToken);

                    if (data != null && data.StatusId != Status.Expired)
                    {
                        plans.Add(new PayToPublishResponseDto
                        {
                            Id = data.Id,
                            PlanName = data.PlanName,
                            Price = data.Price,
                            Currency = data.Currency,
                            Description = data.Description,
                            DurationId = (int)data.Duration,
                            DurationName = GetEnumDisplayName(data.Duration)
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving data for plan ID: {Id}", id);
                }
            }

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
            if (request == null) throw new ArgumentNullException(nameof(request));

            var id = Guid.NewGuid();
            var startDate = DateTime.UtcNow;

            var payToPublishActor = GetActorProxy(request.PayToPublishId);
            var payToPublishData = await payToPublishActor.GetDataAsync(cancellationToken);

            if (payToPublishData == null)
                throw new Exception($"PayToPublish data not found for ID: {request.PayToPublishId}");

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