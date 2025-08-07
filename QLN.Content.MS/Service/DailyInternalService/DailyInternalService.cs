using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.Constants;
using static QLN.Common.Infrastructure.Constants.ConstantValues;
using System.Text.Json;
using QLN.Common.Infrastructure.DTO_s;
using Microsoft.Extensions.Logging;
using Dapr;
using QLN.Common.Infrastructure.CustomException;

namespace QLN.Content.MS.Service.DailyInternalService
{
    public class DailyInternalService : IV2ContentDailyService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<DailyInternalService> _logger;
        private const string Store = ConstantValues.V2Content.ContentStoreName;
        private readonly IV2NewsService _news;
        private readonly IV2EventService _events;

        public DailyInternalService(DaprClient dapr, ILogger<DailyInternalService> logger, IV2NewsService news, IV2EventService events)
        {
            _dapr = dapr;
            _logger = logger;
            _news = news;
            _events = events;
        }
        public async Task<string> UpsertSlotAsync(string userId, DailyTopSectionSlot dto, CancellationToken cancellationToken = default)
        {
            dto.Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category;
            dto.Subcategory = string.IsNullOrWhiteSpace(dto.Subcategory) ? null : dto.Subcategory;

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
        public async Task<List<V2NewsArticleDTO>> GetUnusedDailyTopSectionArticlesAsync(int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            int currentPage = page ?? 1;
            int currentPageSize = pageSize ?? ConstantValues.DefaultPageSize;

            var allArticles = await _news.GetAllNewsFilterArticles(true, cancellationToken)
                           ?? new List<V2NewsArticleDTO>();

            var published = allArticles
                .Where(a => a.Categories.Any(c => c.SlotId < 15) && a.IsActive == true)
                .ToList();

            List<DailyTopSectionSlot> slots;
            try
            {
                var tasks = Enumerable.Range(1, 9).Select(i =>
                    _dapr.GetStateAsync<DailyTopSectionSlot>(
                        Store, $"daily-slot-{i}", cancellationToken: cancellationToken));
                var results = await Task.WhenAll(tasks);
                slots = results.Where(s => s != null).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read daily top‐section slots");
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }

            var usedIds = slots
                .Where(s => s.ContentType == DailyContentType.Article)
                .Select(s => s.RelatedContentId)
                .ToHashSet();

            var unused = published
            .Where(a => !usedIds.Contains(a.Id))
            .OrderByDescending(a => a.PublishedDate ?? a.CreatedAt) 
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .ToList();

            return unused;

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
            var indexKey = $"daily-{dto.TopicId}-slots-index";
            var slotIndex = await _dapr.GetStateAsync<Dictionary<int, Guid>>(
                Store, indexKey, cancellationToken: cancellationToken)
                ?? new Dictionary<int, Guid>();

            dto.Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category;
            dto.Subcategory = string.IsNullOrWhiteSpace(dto.Subcategory) ? null : dto.Subcategory;
            dto.SlotType = 0;

            if (dto.Id != Guid.Empty)
            {
                var existing = await _dapr.GetStateAsync<DailyTopicContent>(
                    Store, dto.Id.ToString(), cancellationToken: cancellationToken);
                if (existing == null)
                    throw new KeyNotFoundException($"No content found with Id {dto.Id}");
                dto.SlotNumber = existing.SlotNumber;
            }
            else
            {
                dto.Id = Guid.NewGuid();

                if (dto.SlotNumber <= 0)
                {
                    int next = slotIndex.Keys.DefaultIfEmpty(0).Max() + 1;
                    dto.SlotNumber = next;
                }
            }
            if (dto.SlotNumber < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(dto.SlotNumber), "SlotNumber must be a positive integer.");

            switch (dto.ContentType)
            {
                case DailyContentType.Video:
                    if (dto.ContentUrl == null)
                        throw new InvalidOperationException(
                            "A video slot must include a ContentUrl.");
                    break;
                case DailyContentType.Article:
                    if (dto.RelatedContentId == Guid.Empty)
                        throw new InvalidOperationException(
                            "An article slot must include Category, Subcategory and a RelatedContentId.");
                    break;
                case DailyContentType.Event:
                    if (dto.RelatedContentId == Guid.Empty)
                        throw new InvalidOperationException(
                            "An event slot must include Category and a RelatedContentId.");
                    break;
                default:
                    throw new InvalidOperationException("Unknown content type.");
            }

            var slotKey = GetSlotKey(dto.TopicId, dto.SlotNumber);
            var occupied = await _dapr.GetStateAsync<DailyTopicContent>(
                Store, slotKey, cancellationToken: cancellationToken);

            if (dto.Id == Guid.Empty)
                dto.Id = Guid.NewGuid();

            if (dto.CreatedAt == default) dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = DateTime.UtcNow;
            dto.CreatedBy ??= userId;
            dto.UpdatedBy = userId;

            await _dapr.SaveStateAsync(Store, slotKey, dto, cancellationToken: cancellationToken);
            await _dapr.SaveStateAsync(Store, dto.Id.ToString(), dto, cancellationToken: cancellationToken);

            await RebuildSlotIndexAsync(dto.TopicId, cancellationToken);
            await RebuildContentIndexAsync(dto.TopicId, cancellationToken);

            return occupied == null
                ? $"Content placed into slot {dto.SlotNumber}."
                : $"Slot {dto.SlotNumber} updated.";
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
        public async Task<List<DailyTopicContent>> GetSlotsByTopicName(string topicName, CancellationToken cancellationToken = default)
        {
            var topics = await GetAllDailyTopicsAsync(cancellationToken);
            var topic = topics
                .FirstOrDefault(t =>
                    t.TopicName.Equals(topicName, StringComparison.OrdinalIgnoreCase)
                );
            if (topic == null)
                throw new KeyNotFoundException($"No daily‐topic found with name '{topicName}'");
            return await GetSlotsByTopicAsync(topic.Id, cancellationToken);
        }
        public async Task<string> ReorderSlotsBatchAsync(string userId,DailyTopicSlotReorderRequest request,CancellationToken cancellationToken = default)
        {
            string store = ConstantValues.V2Content.ContentStoreName;
            var topicKeyPrefix = $"daily-{request.TopicId}-slot";

            if (request.SlotAssignments == null || !request.SlotAssignments.Any())
                throw new InvalidDataException("At least one slot assignment is required.");

            var slotNums = request.SlotAssignments.Select(a => a.SlotNumber).ToList();
            if (slotNums.Any(n => n < 1) || slotNums.Distinct().Count() != slotNums.Count)
                throw new InvalidDataException("SlotNumber must be unique and positive.");

            var loaded = new Dictionary<Guid, DailyTopicContent>();
            foreach (var a in request.SlotAssignments)
            {
                if (!a.DailyId.HasValue)
                    continue;

                var dto = await _dapr.GetStateAsync<DailyTopicContent>(
                    store,
                    a.DailyId.Value.ToString(),
                    cancellationToken: cancellationToken);

                if (dto == null)
                    throw new InvalidDataException($"No content found with Id {a.DailyId}");

                loaded[a.DailyId.Value] = dto;
            }

            var indexKey = $"daily-{request.TopicId}-slots-index";
            var existingIndex = await _dapr.GetStateAsync<Dictionary<int, Guid>>(
                store, indexKey, cancellationToken: cancellationToken)
                ?? new Dictionary<int, Guid>();

            foreach (var oldSlot in existingIndex.Keys)
            {
                var oldKey = $"{topicKeyPrefix}{oldSlot}";
                await _dapr.DeleteStateAsync(store, oldKey, cancellationToken: cancellationToken);
            }

            var newIndex = new Dictionary<int, Guid>();
            foreach (var a in request.SlotAssignments)
            {
                if (!a.DailyId.HasValue)
                    continue;

                var dto = loaded[a.DailyId.Value];
                dto.SlotNumber = a.SlotNumber;
                dto.UpdatedBy = userId;
                dto.UpdatedAt = DateTime.UtcNow;

                var slotKey = $"{topicKeyPrefix}{a.SlotNumber}";
                await _dapr.SaveStateAsync(store, slotKey, dto, cancellationToken: cancellationToken);
                await _dapr.SaveStateAsync(store, dto.Id.ToString(), dto, cancellationToken: cancellationToken);

                newIndex[a.SlotNumber] = a.DailyId.Value;
            }

            await _dapr.SaveStateAsync(store, indexKey, newIndex, cancellationToken: cancellationToken);

            return "Slots reordered successfully.";
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
        public async Task<bool> DeleteDailyTopicAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var key = $"daily-topic:{id}";
            var topic = await _dapr.GetStateAsync<DailyTopic>(V2Content.ContentStoreName, key, cancellationToken: cancellationToken);
            if (topic == null)
                return false;

            topic.IsPublished = false;

            await _dapr.SaveStateAsync(V2Content.ContentStoreName, key, topic, cancellationToken: cancellationToken);
            _logger.LogInformation("Daily topic with ID {Id} was soft-deleted (marked as unpublished).", id);

            return true;
        }
        public async Task<List<V2NewsArticleDTO>> GetUnusedNewsArticlesForTopicAsync(Guid topicId, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            if (topicId == Guid.Empty)
                throw new ArgumentOutOfRangeException(
                    nameof(topicId), "TopicId cannot be empty.");

            int currentPage = page ?? 1;
            int currentPageSize = pageSize ?? ConstantValues.DefaultPageSize;

            var allArticles = await _news.GetAllNewsFilterArticles(true, cancellationToken);

            Dictionary<int, Guid> slotIndex;
            try
            {
                slotIndex = await _dapr.GetStateAsync<Dictionary<int, Guid>>(
                    V2Content.ContentStoreName,
                    $"daily-{topicId}-slots-index",
                    cancellationToken: cancellationToken)
                    ?? new Dictionary<int, Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to read slots-index for topic {TopicId}", topicId);
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }

            var usedIds = new HashSet<Guid>(slotIndex.Values);
            var unused = allArticles
                .Where(a => !usedIds.Contains(a.Id)
                 && a.IsActive
                 && a.Categories.Any(x => x.SlotId != 15))
                .OrderByDescending(a => a.PublishedDate ?? a.CreatedAt)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToList();

            return unused;
        }
        public async Task<ContentsDailyPageResponse> GetDailyLivingLandingAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting GetDailyLivingLandingAsync");

            try
            {
                // Initialize category lookup
                var categoryLookup = await BuildCategoryLookupAsync(ct);

                // Fetch and validate slots
                var slots = await GetAllSlotsAsync(ct) ?? new List<DailyTopSectionSlot>();
                _logger.LogDebug("Retrieved {Count} slots from state store", slots.Count);

                if (!slots.Any())
                {
                    _logger.LogWarning("No slots found in state; landing page will be empty");
                    return CreateEmptyResponse();
                }

                // Sort slots by slot number to ensure proper ordering
                var sortedSlots = slots.OrderBy(s => s.SlotNumber).ToList();

                // Get all published topics first for consistent mapping
                var allPublishedTopics = (await GetAllDailyTopicsAsync(ct))
                               .Where(t => t.IsPublished)
                               .ToList();

                // Process content sequentially to avoid race conditions
                var topStoryResult = await ProcessTopStoryAsync(sortedSlots, categoryLookup, ct);
                var highlightedEventResult = await ProcessHighlightedEventAsync(sortedSlots, ct);
                var topStoriesResult = await ProcessTopStoriesAsync(sortedSlots, categoryLookup, ct);
                var moreArticlesResult = await ProcessMoreArticlesAsync(sortedSlots, categoryLookup, ct);
                var featuredEventsResult = await ProcessFeaturedEventsAsync(ct);
                var topicQueues = await ProcessDynamicTopicsAsync(allPublishedTopics, categoryLookup, ct);

                // Helper to avoid out‐of‐range on topicQueues - now preserves topic names
                BaseQueueResponse<ContentEvent> GetTopicQueue(int idx)
                {
                    if (idx < topicQueues.Count)
                    {
                        return topicQueues[idx];
                    }

                    // Return empty queue but preserve any available topic name
                    var topicName = idx < allPublishedTopics.Count ? allPublishedTopics[idx].TopicName : string.Empty;

                    return new BaseQueueResponse<ContentEvent>
                    {
                        QueueLabel = topicName,
                        Items = new List<ContentEvent>()
                    };
                }

                // Assemble response
                var contents = new ContentsDaily
                {
                    DailyTopStory = topStoryResult,
                    DailyTopStories = topStoriesResult,
                    DailyEvent = highlightedEventResult,
                    DailyFeaturedEvents = featuredEventsResult,
                    DailyMoreArticles = moreArticlesResult,
                    DailyTopics1 = GetTopicQueue(0),
                    DailyTopics2 = GetTopicQueue(1),
                    DailyTopics3 = GetTopicQueue(2),
                    DailyTopics4 = GetTopicQueue(3),
                    DailyTopics5 = GetTopicQueue(4)
                };

                _logger.LogInformation("GetDailyLivingLandingAsync completed successfully");
                return new ContentsDailyPageResponse { ContentsDaily = contents };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetDailyLivingLandingAsync");
                return CreateEmptyResponse();
            }
        }

        private async Task<Dictionary<int, string>> BuildCategoryLookupAsync(CancellationToken ct)
        {
            var allCategoryDtos = await _news.GetAllCategoriesAsync(ct);
            return allCategoryDtos
                .Where(c => !string.IsNullOrWhiteSpace(c.CategoryName))
                .ToDictionary(c => c.Id, c => c.CategoryName);
        }

        private async Task<BaseQueueResponse<ContentPost>> ProcessTopStoryAsync(List<DailyTopSectionSlot> slots, Dictionary<int, string> categoryLookup, CancellationToken ct)
        {
            var topStoryItems = new List<ContentPost>();
            var slot1 = slots.FirstOrDefault(s => s.SlotNumber == 1 && s.ContentType == DailyContentType.Article);

            if (slot1?.RelatedContentId != Guid.Empty)
            {
                var article = await LoadArticleAsync(slot1.RelatedContentId, ct);
                if (article != null)
                {
                    topStoryItems.Add(CreateContentPost(article, categoryLookup, "Top Story"));
                    _logger.LogInformation("Top Story loaded: {Title}", (string)article.Title);
                }
            }
            else
            {
                _logger.LogWarning("Slot 1 (Top Story) is empty, not an Article, or has no RelatedContentId");
            }

            return new BaseQueueResponse<ContentPost>
            {
                QueueLabel = "Top Story",
                Items = topStoryItems
            };
        }

        private async Task<BaseQueueResponse<ContentEvent>> ProcessHighlightedEventAsync(List<DailyTopSectionSlot> slots, CancellationToken ct)
        {
            var highlightedEventItems = new List<ContentEvent>();
            var slot2 = slots.FirstOrDefault(s => s.SlotNumber == 2 && s.ContentType == DailyContentType.Event);

            if (slot2?.RelatedContentId != Guid.Empty)
            {
                var eventItem = await LoadEventAsync(slot2.RelatedContentId, ct);
                if (eventItem != null)
                {
                    highlightedEventItems.Add(CreateContentEvent(eventItem, "Highlighted Event"));
                    _logger.LogInformation("Highlighted Event loaded: {Title}", eventItem.EventTitle);
                }
            }
            else
            {
                _logger.LogWarning("Slot 2 (Highlighted Event) is empty, not an Event, or has no RelatedContentId");
            }

            return new BaseQueueResponse<ContentEvent>
            {
                QueueLabel = "Highlighted Event",
                Items = highlightedEventItems
            };
        }

        private async Task<BaseQueueResponse<ContentPost>> ProcessTopStoriesAsync(List<DailyTopSectionSlot> slots, Dictionary<int, string> categoryLookup, CancellationToken ct)
        {
            var topStoriesItems = new List<ContentPost>();
            var topStorySlots = slots.Where(s => s.SlotNumber is >= 3 and <= 5 && s.ContentType == DailyContentType.Article)
                                    .OrderBy(s => s.SlotNumber)
                                    .ToList();

            foreach (var slot in topStorySlots)
            {
                var article = await LoadArticleAsync(slot.RelatedContentId, ct);
                if (article != null)
                {
                    topStoriesItems.Add(CreateContentPost(article, categoryLookup, "Top Stories"));
                }
            }

            _logger.LogInformation("Top Stories loaded: {Count} items", topStoriesItems.Count);
            return new BaseQueueResponse<ContentPost>
            {
                QueueLabel = "Top Stories",
                Items = topStoriesItems
            };
        }

        private async Task<BaseQueueResponse<ContentEvent>> ProcessMoreArticlesAsync(List<DailyTopSectionSlot> slots, Dictionary<int, string> categoryLookup, CancellationToken ct)
        {
            var moreArticlesItems = new List<ContentEvent>();
            var moreArticleSlots = slots.Where(s => s.SlotNumber is >= 6 and <= 9 && s.ContentType == DailyContentType.Article)
                                       .OrderBy(s => s.SlotNumber)
                                       .ToList();

            foreach (var slot in moreArticleSlots)
            {
                var article = await LoadArticleAsync(slot.RelatedContentId, ct);
                if (article != null)
                {
                    moreArticlesItems.Add(CreateContentEventFromArticle(article, categoryLookup, "More Articles"));
                }
            }

            _logger.LogInformation("More Articles loaded: {Count} items", moreArticlesItems.Count);
            return new BaseQueueResponse<ContentEvent>
            {
                QueueLabel = "More Articles",
                Items = moreArticlesItems
            };
        }

        private async Task<BaseQueueResponse<ContentEvent>> ProcessFeaturedEventsAsync(CancellationToken ct)
        {
            var featuredEventItems = new List<ContentEvent>();
            _logger.LogDebug("Loading all featured events");

            var featuredEvents = await _events.GetAllIsFeaturedEvents(true, ct) ?? new List<V2Events>();
            var sortedFeaturedEvents = featuredEvents
       .OrderBy(e => e.FeaturedSlot?.Id ?? int.MaxValue)
       .ToList();

            foreach (var eventItem in sortedFeaturedEvents)
            {
                featuredEventItems.Add(CreateContentEvent(eventItem, "Featured Events"));
            }

            _logger.LogInformation("Featured Events loaded: {Count}", featuredEventItems.Count);
            return new BaseQueueResponse<ContentEvent>
            {
                QueueLabel = "Featured Events",
                Items = featuredEventItems
            };
        }

        private async Task<List<BaseQueueResponse<ContentEvent>>> ProcessDynamicTopicsAsync(IEnumerable<dynamic> publishedTopics, Dictionary<int, string> categoryLookup, CancellationToken ct)
        {
            var allTopics = (await GetAllDailyTopicsAsync(ct))
                            .Where(t => t.IsPublished)
                            .ToList();

            var topicQueues = new List<BaseQueueResponse<ContentEvent>>();
            _logger.LogDebug("Building queues for {Count} published topics", allTopics.Count);

            foreach (var topic in allTopics)
            {
                var items = new List<ContentEvent>();
                var topicSlots = (await GetSlotsByTopicAsync(topic.Id, ct) ?? new List<DailyTopicContent>())
                                .OrderBy(s => s.SlotNumber)
                                .ToList();

                _logger.LogDebug("Topic '{Topic}' has {Count} slots", topic.TopicName, topicSlots.Count);

                foreach (var slot in topicSlots)
                {
                    var contentEvent = await ProcessTopicSlotAsync(slot, topic.TopicName, categoryLookup, ct);
                    if (contentEvent != null)
                    {
                        items.Add(contentEvent);
                    }
                }

                topicQueues.Add(new BaseQueueResponse<ContentEvent>
                {
                    QueueLabel = topic.TopicName,
                    Items = items
                });

                _logger.LogInformation("Topic '{Topic}' queue built with {Count} items", topic.TopicName, items.Count);
            }

            return topicQueues;
        }

        private async Task<ContentEvent> ProcessTopicSlotAsync(DailyTopicContent slot, string topicName, Dictionary<int, string> categoryLookup, CancellationToken ct)
        {
            return slot.ContentType switch
            {
                DailyContentType.Article => await ProcessTopicArticleAsync(slot, topicName, categoryLookup, ct),
                DailyContentType.Event => await ProcessTopicEventAsync(slot, topicName, ct),
                DailyContentType.Video => ProcessTopicVideo(slot, topicName),
                _ => null
            };
        }

        private async Task<ContentEvent> ProcessTopicArticleAsync(DailyTopicContent slot, string topicName, Dictionary<int, string> categoryLookup, CancellationToken ct)
        {
            _logger.LogDebug("Topic {Topic}: loading Article {Id}", topicName, slot.RelatedContentId);
            var article = await LoadArticleAsync(slot.RelatedContentId, ct);

            if (article == null)
            {
                _logger.LogWarning("Topic {Topic}: Article {Id} not found", topicName, slot.RelatedContentId);
                return null;
            }

            return CreateContentEventFromArticle(article, categoryLookup, topicName);
        }

        private async Task<ContentEvent> ProcessTopicEventAsync(DailyTopicContent slot, string topicName, CancellationToken ct)
        {
            _logger.LogDebug("Topic {Topic}: loading Event {Id}", topicName, slot.RelatedContentId);
            var eventItem = await LoadEventAsync(slot.RelatedContentId, ct);

            if (eventItem == null)
            {
                _logger.LogWarning("Topic {Topic}: Event {Id} not found", topicName, slot.RelatedContentId);
                return null;
            }

            return CreateContentEvent(eventItem, topicName);
        }

        private ContentEvent ProcessTopicVideo(DailyTopicContent slot, string topicName)
        {
            _logger.LogDebug("Topic {Topic}: adding Video slot {Slot}", topicName, slot.SlotNumber);
            return new ContentEvent
            {
                Id = slot.Id,
                IsActive = true,
                CreatedAt = slot.CreatedAt,
                UpdatedAt = slot.UpdatedAt,
                PageName = DrupalContentConstants.QlnContentsDaily,
                Nid = slot.RelatedContentId.ToString(),
                Title = slot.Title,
                Category = slot.Category,
                DateCreated = slot.PublishedDate.ToString("o"),
                ImageUrl = slot.ContentUrl,
                Slug = slot.ContentUrl,
                NodeType = "video"
            };
        }

        private async Task<dynamic> LoadArticleAsync(Guid articleId, CancellationToken ct)
        {
            if (articleId == Guid.Empty)
            {
                _logger.LogWarning("Cannot load article with empty ID");
                return null;
            }

            try
            {
                return await _news.GetArticleByIdAsync(articleId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading article {ArticleId}", articleId);
                return null;
            }
        }

        private async Task<V2Events> LoadEventAsync(Guid eventId, CancellationToken ct)
        {
            if (eventId == Guid.Empty)
            {
                _logger.LogWarning("Cannot load event with empty ID");
                return null;
            }

            try
            {
                return await _events.GetEventById(eventId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event {EventId}", eventId);
                return null;
            }
        }

        private ContentPost CreateContentPost(dynamic article, Dictionary<int, string> categoryLookup, string queueLabel)
        {
            // Handle Categories collection properly with explicit casting
            var categories = (IEnumerable<dynamic>)article.Categories ?? Enumerable.Empty<dynamic>();
            var catId = categories.FirstOrDefault()?.CategoryId ?? default(int);
            var can = new CancellationToken();
            var AllCommentCount = _news.GetCommentsByArticleIdAsync((string)article.Id, null, null, can).Result.TotalComments;


            return new ContentPost
            {
                Id = (Guid)article.Id,
                Description = (string)article.Content,
                CreatedAt = (DateTime)article.CreatedAt,
                UpdatedAt = (DateTime)article.UpdatedAt,
                IsActive = (bool)article.IsActive,
                PageName = DrupalContentConstants.QlnContentsDaily,
                QueueLabel = queueLabel,
                NodeType = "post",
                Nid = article.Id.ToString(),
                DateCreated = ((DateTime)article.CreatedAt).ToString("o"),
                ImageUrl = (string)article.CoverImageUrl,
                UserName = (string)article.authorName,
                Title = (string)article.Title,
                Slug = (string)article.Slug,
                WriterTag = article.WriterTag,
                CommentsCounts = AllCommentCount,
                Category = categoryLookup.ContainsKey(catId) ? categoryLookup[catId] : string.Empty
            };
        }

        private ContentEvent CreateContentEvent(V2Events eventItem, string queueLabel)
        {
            return new ContentEvent
            {
                Id = eventItem.Id,
                IsActive = eventItem.IsActive,
                CreatedAt = eventItem.CreatedAt,
                UpdatedAt = eventItem.UpdatedAt,
                PageName = DrupalContentConstants.QlnContentsDaily,
                QueueLabel = queueLabel,
                NodeType = "event",
                Nid = eventItem.Id.ToString(),
                DateCreated = eventItem.CreatedAt.ToString("o"),
                ImageUrl = eventItem.CoverImage,
                UserName = eventItem.CreatedBy,
                Title = eventItem.EventTitle,
                Slug = eventItem.Slug,
                EventCategory = eventItem.CategoryName,
                EventVenue = eventItem.Venue,
                EventStart = eventItem.EventSchedule?.StartDate.ToString("o") ?? string.Empty,
                EventEnd = eventItem.EventSchedule?.EndDate.ToString("o") ?? string.Empty,
                EventLat = eventItem.Latitude,
                EventLong = eventItem.Longitude,
                EventLocation = eventItem.Location,
                Description = eventItem.EventDescription
            };
        }

        private ContentEvent CreateContentEventFromArticle(dynamic article, Dictionary<int, string> categoryLookup, string queueLabel)
        {
            // Handle Categories collection properly with explicit casting
            var categories = (IEnumerable<dynamic>)article.Categories ?? Enumerable.Empty<dynamic>();
            var catId = categories.FirstOrDefault()?.CategoryId ?? default(int);
            var can = new CancellationToken();
            var AllCommentCount = _news.GetCommentsByArticleIdAsync((string)article.Id, null, null, can).Result.TotalComments;

            return new ContentEvent
            {
                Id = (Guid)article.Id,
                CreatedAt = (DateTime)article.CreatedAt,
                UpdatedAt = (DateTime)article.UpdatedAt,
                IsActive = (bool)article.IsActive,
                Description = (string)article.Content,
                UserName = (string)article.authorName,
                PageName = DrupalContentConstants.QlnContentsDaily,
                QueueLabel = queueLabel,
                Nid = article.Id.ToString(),
                Title = (string)article.Title,
                Category = categoryLookup.ContainsKey(catId) ? categoryLookup[catId] : string.Empty,
                DateCreated = ((DateTime)article.CreatedAt).ToString("o"),
                ImageUrl = (string)article.CoverImageUrl,
                Slug = (string)article.Slug,
                NodeType = "post",
                CommentsCounts = AllCommentCount
            };
        }

        private ContentsDailyPageResponse CreateEmptyResponse()
        {
            return new ContentsDailyPageResponse
            {
                ContentsDaily = new ContentsDaily
                {
                    DailyTopStory = new BaseQueueResponse<ContentPost> { QueueLabel = "Top Story", Items = new List<ContentPost>() },
                    DailyTopStories = new BaseQueueResponse<ContentPost> { QueueLabel = "Top Stories", Items = new List<ContentPost>() },
                    DailyEvent = new BaseQueueResponse<ContentEvent> { QueueLabel = "Highlighted Event", Items = new List<ContentEvent>() },
                    DailyFeaturedEvents = new BaseQueueResponse<ContentEvent> { QueueLabel = "Featured Events", Items = new List<ContentEvent>() },
                    DailyMoreArticles = new BaseQueueResponse<ContentEvent> { QueueLabel = "More Articles", Items = new List<ContentEvent>() },
                    DailyTopics1 = new BaseQueueResponse<ContentEvent> { QueueLabel = string.Empty, Items = new List<ContentEvent>() },
                    DailyTopics2 = new BaseQueueResponse<ContentEvent> { QueueLabel = string.Empty, Items = new List<ContentEvent>() },
                    DailyTopics3 = new BaseQueueResponse<ContentEvent> { QueueLabel = string.Empty, Items = new List<ContentEvent>() },
                    DailyTopics4 = new BaseQueueResponse<ContentEvent> { QueueLabel = string.Empty, Items = new List<ContentEvent>() },
                    DailyTopics5 = new BaseQueueResponse<ContentEvent> { QueueLabel = string.Empty, Items = new List<ContentEvent>() }
                }
            };
        }
    }
}
