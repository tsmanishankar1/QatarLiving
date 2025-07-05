using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.Constants;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.DailyInternalService
{
    public class DailyInternalService : IV2ContentDailyService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<DailyInternalService> _logger;
        private const string Store = ConstantValues.V2Content.ContentStoreName;

        public DailyInternalService(DaprClient dapr, ILogger<DailyInternalService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }
        public async Task<string> UpsertSlotAsync(string userId, DailyTopSectionSlot dto,CancellationToken cancellationToken = default)
        {

            if (dto.SlotNumber < 1 || dto.SlotNumber > 9)
                throw new ArgumentOutOfRangeException(nameof(dto.SlotNumber), "Slot must be 1–9");
            if (!Enum.IsDefined(typeof(DailySlotType), dto.SlotType))
                throw new ArgumentOutOfRangeException(
                    nameof(dto.SlotType),
                    $"Invalid slot type: {dto.SlotType}");
            if (!Enum.IsDefined(typeof(DailyContentType), dto.ContentType))
                throw new ArgumentOutOfRangeException(
                    nameof(dto.ContentType),
                    $"Invalid content type: {dto.ContentType}");

            if (dto.SlotType == DailySlotType.TopStory && dto.ContentType != DailyContentType.Article)
                throw new InvalidOperationException("Slot 1 (TopStory) must be an Article");
            if (dto.SlotType == DailySlotType.HighlightedEvent && dto.ContentType != DailyContentType.Event)
                throw new InvalidOperationException("Slot 2 (HighlightedEvent) must be an Event");
            if (dto.SlotNumber >= 3 && dto.SlotNumber <= 9 && dto.ContentType != DailyContentType.Article)
                throw new InvalidOperationException("Slots 3–9 (Articles) must be an Article");

            var key = $"daily-slot-{dto.SlotNumber}";
            var existing = await _dapr.GetStateAsync<DailyTopSectionSlot>(Store, key, cancellationToken: cancellationToken);

            if (existing is null)
            {
                dto.Id = Guid.NewGuid();
                dto.CreatedAt = DateTime.UtcNow;
                dto.CreatedBy = userId;
            }
            else
            {
                dto.Id = existing.Id;
                dto.UpdatedBy = userId;
                dto.UpdatedAt = DateTime.UtcNow;
            }
            await _dapr.SaveStateAsync(Store, key, dto, cancellationToken: cancellationToken);

            return existing is null
                ? "Slot created successfully"
                : "Slot updated successfully";
        }
        public async Task<List<DailyTopSectionSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)
        {
            var slots = new List<DailyTopSectionSlot>();

            for (int slotNumber = 1; slotNumber <= 9; slotNumber++)
            {
                var key = $"daily-slot-{slotNumber}";
                var dto = await _dapr.GetStateAsync<DailyTopSectionSlot>(
                    Store, key, cancellationToken: cancellationToken);

                if (dto is not null)
                {
                    slots.Add(dto);
                }
            }

            return slots;
        }
    }
}
