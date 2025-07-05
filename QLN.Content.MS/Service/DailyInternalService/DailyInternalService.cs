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
        public async Task<string> UpsertSlotAsync( int slotNumber,DailyTopSectionSlot dto,CancellationToken cancellationToken = default)
        {
            // 1) enforce 1–9
            if (slotNumber < 1 || slotNumber > 9)
                throw new ArgumentOutOfRangeException(nameof(slotNumber), "Slot must be 1–9");

            // 2) enforce content‐type
            var slotType = (DailySlotType)slotNumber;
            if (slotType == DailySlotType.TopStory && dto.ContentType != DailyContentType.Article)
                throw new InvalidOperationException("Slot 1 (TopStory) must be an Article");
            if (slotType == DailySlotType.HighlightedEvent && dto.ContentType != DailyContentType.Event)
                throw new InvalidOperationException("Slot 2 (HighlightedEvent) must be an Event");
            if (slotNumber >= 3 && slotNumber <= 9 && dto.ContentType != DailyContentType.Article)
                throw new InvalidOperationException("Slots 3–9 (Articles) must be an Article");

            // 3) load existing
            var key = $"daily-slot-{slotNumber}";
            var existing = await _dapr.GetStateAsync<DailyTopSectionSlot>(
                Store, key, cancellationToken: cancellationToken);

            if (existing is null)
            {
                // new slot
                dto.Id = Guid.NewGuid();
                dto.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // update existing: preserve creation data
                dto.Id = existing.Id;
                dto.CreatedBy = existing.CreatedBy;
                dto.CreatedAt = existing.CreatedAt;
            }

            // 4) stamp metadata
            dto.SlotType = slotType;
            dto.SlotNumber = slotNumber;
            dto.UpdatedAt = DateTime.UtcNow;

            // 5) save
            await _dapr.SaveStateAsync(Store, key, dto, cancellationToken: cancellationToken);

            return existing is null
                ? "Slot created successfully"
                : "Slot updated successfully";
        }
        public async Task<List<DailyTopSectionSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)
        {
            var slots = new List<DailyTopSectionSlot>();

            // Try each of the 9 keys; only add if present
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
        public async Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            topic.Id = topic.Id == Guid.Empty ? Guid.NewGuid() : topic.Id;
            var key = $"daily-topic:{topic.Id}";

            // Save topic
            await _dapr.SaveStateAsync(V2Content.ContentStoreName, key, topic, cancellationToken: cancellationToken);

            // Maintain index
            var index = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.DailyTopicIndexKey, cancellationToken: cancellationToken)
                         ?? new List<string>();

            if (!index.Contains(key))
            {
                index.Add(key);
                await _dapr.SaveStateAsync(V2Content.ContentStoreName, V2Content.DailyTopicIndexKey, index, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Daily topic {TopicName} with Id {Id} saved", topic.TopicName, topic.Id);
        }
    }
}
