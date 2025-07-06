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
        public async Task<string> CreateContentAsync(string userId, DailyTopicContent dto, CancellationToken cancellationToken = default)
        {
            if (dto.SlotNumber < 1 || dto.SlotNumber > 9)
                throw new ArgumentOutOfRangeException(nameof(dto.SlotNumber), "Slot must be between 1 and 9");
            switch (dto.ContentType)
            {
                case DailyContentType.Video:
                    if (dto.ContentUrl == null)
                        throw new InvalidOperationException("A video slot must include a RelatedContentId (the video URL reference).");
                    break;

                case DailyContentType.Article:
                    if (string.IsNullOrWhiteSpace(dto.Category) ||
                        string.IsNullOrWhiteSpace(dto.Subcategory) ||
                        dto.RelatedContentId == null)
                    {
                        throw new InvalidOperationException(
                            "An article slot must include Category, Subcategory and a RelatedContentId.");
                    }
                    break;

                case DailyContentType.Event:
                    if (string.IsNullOrWhiteSpace(dto.Category) ||
                        dto.RelatedContentId == null)
                    {
                        throw new InvalidOperationException(
                            "An event slot must include Category and a RelatedContentId.");
                    }
                    break;

                default:
                    throw new InvalidOperationException("Unknown content type.");
            }

            var desiredKey = GetSlotKey(dto.TopicId, dto.SlotNumber);
            var occupied = await _dapr.GetStateAsync<DailyTopicContent>(Store, desiredKey, cancellationToken: cancellationToken);
            if (occupied != null)
                await HandleSlotShiftAsync(dto.TopicId, dto.SlotNumber, cancellationToken);

            dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
            if (dto.CreatedAt == default) dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = DateTime.UtcNow;
            dto.CreatedBy ??= userId;
            dto.UpdatedBy = userId;

            await _dapr.SaveStateAsync(Store, desiredKey, dto, cancellationToken: cancellationToken);
            await _dapr.SaveStateAsync(Store, dto.Id.ToString(), dto, cancellationToken: cancellationToken);

            await RebuildSlotIndexAsync(dto.TopicId, cancellationToken);
            await RebuildContentIndexAsync(dto.TopicId, cancellationToken);

            return occupied == null
                ? $"Content placed into slot {dto.SlotNumber}"
                : $"Content inserted into slot {dto.SlotNumber}, downstream items shifted (overflow removed)";
        }
        public async Task<List<DailyTopicContent>> GetSlotsByTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
        {
            var slotIndexKey = $"daily-{topicId}-slots-index";
            var slotIndex = await _dapr.GetStateAsync<Dictionary<int, Guid>>(Store, slotIndexKey, cancellationToken: cancellationToken)
                            ?? new Dictionary<int, Guid>();

            var result = new List<DailyTopicContent>();
            for (int slot = 1; slot <= 9; slot++)
            {
                if (slotIndex.TryGetValue(slot, out var contentId))
                {
                    var content = await _dapr.GetStateAsync<DailyTopicContent>(
                        Store, GetSlotKey(topicId, slot), cancellationToken: cancellationToken);
                    if (content != null)
                        result.Add(content);
                }
            }

            return result;
        }
        public async Task<string> ReorderSlotsAsync(string userId, ReorderDailyTopicContentDto dto, CancellationToken cancellationToken)
        {
            const int MaxSlot = 9;
            if (dto.FromSlot < 1 || dto.FromSlot > MaxSlot || dto.ToSlot < 1 || dto.ToSlot > MaxSlot)
                throw new InvalidDataException("FromSlot and ToSlot must be between 1 and 9.");

            if (dto.FromSlot == dto.ToSlot)
                return $"No changes needed. Content is already in slot {dto.ToSlot}.";

            var storeName = V2Content.ContentStoreName;
            var fromKey = GetSlotKey(dto.TopicId, dto.FromSlot);
            var fromContent = await _dapr.GetStateAsync<DailyTopicContent>(storeName, fromKey, cancellationToken: cancellationToken);
            if (fromContent == null)
                throw new InvalidDataException($"No content found in slot {dto.FromSlot}.");

            var updatedSlots = new List<int>();
            var missingSlots = new List<int>();

            if (dto.FromSlot < dto.ToSlot)
            {
                for (int i = dto.FromSlot + 1; i <= dto.ToSlot; i++)
                {
                    var curKey = GetSlotKey(dto.TopicId, i);
                    var prevKey = GetSlotKey(dto.TopicId, i - 1);
                    var content = await _dapr.GetStateAsync<DailyTopicContent>(storeName, curKey, cancellationToken: cancellationToken);

                    if (content == null)
                    {
                        missingSlots.Add(i);
                        continue;
                    }

                    content.SlotNumber = i - 1;
                    content.UpdatedBy = dto.UserId;
                    content.UpdatedAt = DateTime.UtcNow;

                    await _dapr.SaveStateAsync(storeName, prevKey, content, cancellationToken: cancellationToken);
                    await _dapr.SaveStateAsync(storeName, content.Id.ToString(), content, cancellationToken: cancellationToken);
                    updatedSlots.Add(i - 1);
                }
            }
            else
            {
                for (int i = dto.FromSlot - 1; i >= dto.ToSlot; i--)
                {
                    var curKey = GetSlotKey(dto.TopicId, i);
                    var nextKey = GetSlotKey(dto.TopicId, i + 1);
                    var content = await _dapr.GetStateAsync<DailyTopicContent>(storeName, curKey, cancellationToken: cancellationToken);

                    if (content == null)
                    {
                        missingSlots.Add(i);
                        continue;
                    }

                    content.SlotNumber = i + 1;
                    content.UpdatedBy = dto.UserId;
                    content.UpdatedAt = DateTime.UtcNow;

                    await _dapr.SaveStateAsync(storeName, nextKey, content, cancellationToken: cancellationToken);
                    await _dapr.SaveStateAsync(storeName, content.Id.ToString(), content, cancellationToken: cancellationToken);
                    updatedSlots.Add(i + 1);
                }
            }

            var toKey = GetSlotKey(dto.TopicId, dto.ToSlot);
            fromContent.SlotNumber = dto.ToSlot;
            fromContent.UpdatedBy = dto.UserId;
            fromContent.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(storeName, toKey, fromContent, cancellationToken: cancellationToken);
            await _dapr.SaveStateAsync(storeName, fromContent.Id.ToString(), fromContent, cancellationToken: cancellationToken);

            await _dapr.DeleteStateAsync(storeName, fromKey, cancellationToken: cancellationToken);
            updatedSlots.Add(dto.ToSlot);

            await RebuildSlotIndexAsync(dto.TopicId, cancellationToken);
            await RebuildContentIndexAsync(dto.TopicId, cancellationToken);

            updatedSlots.Sort();
            var result = $"Reordered successfully. Updated slots: {string.Join(", ", updatedSlots)}.";
            if (missingSlots.Any())
                result += $" Warning: slots {string.Join(", ", missingSlots)} were empty and skipped.";

            return result;
        }

        public async Task<string> DeleteContentAsync(Guid contentId, CancellationToken cancellationToken)
        {
            var dto = await _dapr.GetStateAsync<DailyTopicContent>(
                Store, contentId.ToString(), cancellationToken: cancellationToken
            );
            if (dto == null)
                throw new KeyNotFoundException($"No content found with Id {contentId}");

            var slotKey = GetSlotKey(dto.TopicId, dto.SlotNumber);
            await _dapr.DeleteStateAsync(Store, slotKey, cancellationToken: cancellationToken);
            await _dapr.DeleteStateAsync(Store, contentId.ToString(), cancellationToken: cancellationToken);

            for (int s = dto.SlotNumber + 1; s <= 9; s++)
            {
                var fromKey = GetSlotKey(dto.TopicId, s);
                var content = await _dapr.GetStateAsync<DailyTopicContent>(
                    Store, fromKey, cancellationToken: cancellationToken
                );
                if (content == null)
                    break; 

                content.SlotNumber = s - 1;
                content.UpdatedAt = DateTime.UtcNow;

                var toKey = GetSlotKey(dto.TopicId, s - 1);
                await _dapr.SaveStateAsync(Store, toKey, content, cancellationToken: cancellationToken);
                await _dapr.SaveStateAsync(Store, content.Id.ToString(), content, cancellationToken: cancellationToken);
                await _dapr.DeleteStateAsync(Store, fromKey, cancellationToken: cancellationToken);
            }
            await RebuildSlotIndexAsync(dto.TopicId, cancellationToken);
            await RebuildContentIndexAsync(dto.TopicId, cancellationToken);

            return "Content deleted and slots collapsed";
        }
        private async Task HandleSlotShiftAsync(Guid topicId, int desiredSlot, CancellationToken cancellationToken)
        {
            const int MaxSlot = 9;
            int emptySlot = -1;
            for (int i = desiredSlot + 1; i <= MaxSlot; i++)
            {
                var key = GetSlotKey(topicId, i);
                var state = await _dapr.GetStateAsync<DailyTopicContent>(Store, key, cancellationToken: cancellationToken);
                if (state == null)
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot == -1)
            {
                var lastKey = GetSlotKey(topicId, MaxSlot);
                var last = await _dapr.GetStateAsync<DailyTopicContent>(Store, lastKey, cancellationToken: cancellationToken);
                if (last != null)
                {
                    await _dapr.DeleteStateAsync(Store, lastKey, cancellationToken: cancellationToken);
                    await _dapr.DeleteStateAsync(Store, last.Id.ToString(), cancellationToken: cancellationToken);
                }
                emptySlot = MaxSlot;
            }

            for (int slot = emptySlot - 1; slot >= desiredSlot; slot--)
            {
                var fromKey = GetSlotKey(topicId, slot);
                var toKey = GetSlotKey(topicId, slot + 1);

                var content = await _dapr.GetStateAsync<DailyTopicContent>(Store, fromKey, cancellationToken: cancellationToken);
                if (content != null)
                {
                    content.SlotNumber = slot + 1;
                    content.UpdatedBy = content.UpdatedBy; 
                    content.UpdatedAt = DateTime.UtcNow;

                    await _dapr.SaveStateAsync(Store, toKey, content, cancellationToken: cancellationToken);
                    await _dapr.SaveStateAsync(Store, content.Id.ToString(), content, cancellationToken: cancellationToken);

                    await _dapr.DeleteStateAsync(Store, fromKey, cancellationToken: cancellationToken);
                }
            }
        }
        private async Task RebuildSlotIndexAsync(Guid topicId, CancellationToken ct)
        {
            var dict = new Dictionary<int, Guid>();
            for (int slot = 1; slot <= 9; slot++)
            {
                var key = GetSlotKey(topicId, slot);
                var c = await _dapr.GetStateAsync<DailyTopicContent>(Store, key, cancellationToken: ct);
                if (c != null)
                    dict[slot] = c.Id;
            }
            await _dapr.SaveStateAsync(Store, $"daily-{topicId}-slots-index", dict, cancellationToken: ct);
        }

        private async Task RebuildContentIndexAsync(Guid topicId, CancellationToken ct)
        {
            var list = new List<Guid>();
            for (int slot = 1; slot <= 9; slot++)
            {
                var key = GetSlotKey(topicId, slot);
                var c = await _dapr.GetStateAsync<DailyTopicContent>(Store, key, cancellationToken: ct);
                if (c != null)
                    list.Add(c.Id);
            }
            await _dapr.SaveStateAsync(Store, $"daily-{topicId}-index", list, cancellationToken: ct);
        }

        private string GetSlotKey(Guid topicId, int slot) =>
            $"daily-{topicId}-slot{slot}";

    }
}
