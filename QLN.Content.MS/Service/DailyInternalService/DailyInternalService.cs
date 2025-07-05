using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.Constants;
using static QLN.Common.Infrastructure.Constants.ConstantValues;
using System.Text.Json;

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
        public async Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            topic.Id = Guid.NewGuid();
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

        public async Task<List<DailyTopic>> GetAllDailyTopicsAsync(CancellationToken cancellationToken = default)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.DailyTopicIndexKey, cancellationToken: cancellationToken)
                        ?? new List<string>();

            var stateItems = await _dapr.GetBulkStateAsync(
                V2Content.ContentStoreName,
                keys,
                parallelism: null,
                metadata: null,
                cancellationToken: cancellationToken);

            var topics = stateItems
                .Select(s => JsonSerializer.Deserialize<DailyTopic>(s.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                .Where(t => t != null && t.IsActive?.ToLowerInvariant() != "false")
                .ToList();

            return topics!;
        }
        public async Task<bool> UpdateDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            var key = $"daily-topic:{topic.Id}";

            // Check if topic exists
            var existing = await _dapr.GetStateAsync<DailyTopic>(V2Content.ContentStoreName, key, cancellationToken: cancellationToken);
            if (existing == null)
                return false;

            // Overwrite with new data
            await _dapr.SaveStateAsync(V2Content.ContentStoreName, key, topic, cancellationToken: cancellationToken);

            // Update index only if missing
            var index = await _dapr.GetStateAsync<List<string>>(V2Content.ContentStoreName, V2Content.DailyTopicIndexKey, cancellationToken: cancellationToken)
                ?? new List<string>();

            if (!index.Contains(key))
            {
                index.Add(key);
                await _dapr.SaveStateAsync(V2Content.ContentStoreName, V2Content.DailyTopicIndexKey, index, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Daily topic {TopicName} with Id {Id} updated", topic.TopicName, topic.Id);
            return true;
        }

        public async Task<bool> UpdatePublishStatusAsync(Guid id, bool isPublished, CancellationToken cancellationToken = default)
        {
            var key = $"daily-topic:{id}";

            var topic = await _dapr.GetStateAsync<DailyTopic>(V2Content.ContentStoreName, key, cancellationToken: cancellationToken);
            if (topic == null)
                return false;

            topic.IsPublished = isPublished;

            await _dapr.SaveStateAsync(V2Content.ContentStoreName, key, topic, cancellationToken: cancellationToken);

            _logger.LogInformation("DailyTopic {Id} publish status updated to {Status}", id, isPublished);
            return true;
        }

        public async Task<bool> SoftDeleteDailyTopicAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var key = $"daily-topic:{id}";
            var topic = await _dapr.GetStateAsync<DailyTopic>(V2Content.ContentStoreName, key, cancellationToken: cancellationToken);
            if (topic == null)
                return false;

            topic.IsActive = "false";

            await _dapr.SaveStateAsync(V2Content.ContentStoreName, key, topic, cancellationToken: cancellationToken);
            _logger.LogInformation("Daily topic with ID {Id} was soft-deleted.", id);

            return true;
        }


    }
}
