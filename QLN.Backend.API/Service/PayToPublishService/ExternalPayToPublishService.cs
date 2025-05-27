using Dapr.Actors.Client;
using Dapr.Actors;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IPayToPublicActor;
using System.Collections.Concurrent;
using QLN.Common.Infrastructure.IService.IPayToPublishService;
using QLN.Common.Infrastructure.Subscriptions;


namespace QLN.Backend.API.Service.PayToPublishService
{
    public class ExternalPayToPublishService : IPayToPublishService
    {
        private readonly ILogger<ExternalPayToPublishService> _logger;
        private static readonly ConcurrentDictionary<Guid, byte> _payToPublishIds = new();

        public ExternalPayToPublishService(ILogger<ExternalPayToPublishService> logger)
        {
            _logger = logger;
        }

        private IPayToPublishActor GetActorProxy(Guid id)
        {
            return ActorProxy.Create<IPayToPublishActor>(new ActorId(id.ToString()), "PayToPublishActor");
        }

        public async Task CreatePlanAsync(PayToPublishRequestDto request, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();

            var dto = new PayToPublishDto
            {
                Id = id,
                PlanName = request.PlanName,
                TotalCount=request.TotalCount,
                Description=request.Description,
                Duration = request.Duration,
                Price = request.Price,
                Currency = request.Currency,
                VerticalTypeId = request.VerticalTypeId,
                CategoryId = request.CategoryId,
                StatusId = request.StatusId,
                LastUpdated = DateTime.UtcNow
            };

            var actor = GetActorProxy(id);
            var result = await actor.SetDataAsync(dto, cancellationToken);

            if (result)
            {
                _payToPublishIds.TryAdd(id, 0);
            }
            else
            {
                throw new Exception("Pay to publish plan creation failed.");
            }
        }
        public async Task<PayToPublishListResponseDto> GetPlansByVerticalAndCategoryAsync(
            int verticalTypeId,
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            var resultList = new List<PayToPublishResponseDto>();
            var ids = _payToPublishIds.Keys.ToList();

            var verticalEnum = (Vertical)verticalTypeId;
            var categoryEnum = (SubscriptionCategory)categoryId;

            foreach (var id in ids)
            {
                var actor = GetActorProxy(id);
                var data = await actor.GetDataAsync(cancellationToken);

                if (data != null &&
                    data.VerticalTypeId == verticalEnum &&
                    data.CategoryId == categoryEnum &&
                    data.StatusId != Status.Expired)
                {
                    resultList.Add(new PayToPublishResponseDto
                    {
                        Id = data.Id,
                        PlanName = data.PlanName,
                        Price = data.Price,
                        Currency = data.Currency,
                        Description = data.Description,
                        Duration = data.Duration
                    });
                }
            }

            return new PayToPublishListResponseDto
            {
                VerticalId = verticalTypeId,
                VerticalName = verticalEnum.ToString(),
                CategoryId = categoryId,
                CategoryName = categoryEnum.ToString(),
                PayToPublish = resultList
            };
        }

        public async Task<List<PayToPublishResponseDto>> GetAllPlansAsync(CancellationToken cancellationToken = default)
        {
            var ids = _payToPublishIds.Keys.ToList();
            var plans = new List<PayToPublishResponseDto>();

            foreach (var id in ids)
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
                        Duration = data.Duration
                    });
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
            existingData.Duration = request.Duration;
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

            var durationText = payToPublishData.Duration;
            var endDate = ParseDurationAndGetEndDate(startDate, durationText);

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
                LastUpdated = DateTime.UtcNow
            };

            var actor = GetPaymentActorProxy(dto.Id);
            var result = await actor.FastSetDataAsync(dto, cancellationToken);

            if (result)
            {
                _payToPublishIds.TryAdd(dto.Id, 0);
                _logger.LogInformation("Payment transaction created with ID: {TransactionId}", dto.Id);
                return dto.Id;
            }

            throw new Exception("Payment transaction creation failed.");
        }

        private DateTime ParseDurationAndGetEndDate(DateTime startDate, string duration)
        {
            if (string.IsNullOrWhiteSpace(duration))
                throw new ArgumentException("Duration is empty or null", nameof(duration));

            duration = duration.ToLowerInvariant();
            var digits = new string(duration.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(digits))
                throw new ArgumentException($"No digits found in duration: {duration}");

            int value = int.Parse(digits);

            if (duration.Contains("month"))
            {
                return startDate.AddMonths(value);
            }

            if (duration.Contains("year"))
            {
                return startDate.AddYears(value);
            }

            throw new ArgumentException($"Unsupported duration format: {duration}");
        }



        private IPaymentActor GetPaymentActorProxy(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty", nameof(id));

            return ActorProxy.Create<IPaymentActor>(
                new ActorId(id.ToString()),
                "PayToPublishPaymentActor");
        }

    }
}
