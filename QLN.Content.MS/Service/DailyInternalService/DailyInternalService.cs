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
        public async Task<List<V2NewsArticleDTO>> GetUnusedDailyTopSectionArticlesAsync(CancellationToken cancellationToken = default)
        {
            var allArticles = await _news.GetAllNewsFilterArticles(true, cancellationToken)
                           ?? new List<V2NewsArticleDTO>();

            var published = allArticles
                .Where(a => a.Categories.Any(c => c.SlotId < 15))
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

            return published
                .Where(a => !usedIds.Contains(a.Id))
                .ToList();
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
                    if (string.IsNullOrWhiteSpace(dto.Category) ||
                        string.IsNullOrWhiteSpace(dto.Subcategory) ||
                        dto.RelatedContentId == Guid.Empty)
                        throw new InvalidOperationException(
                            "An article slot must include Category, Subcategory and a RelatedContentId.");
                    break;
                case DailyContentType.Event:
                    if (string.IsNullOrWhiteSpace(dto.Category) ||
                        dto.RelatedContentId == Guid.Empty)
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
        public async Task<List<V2NewsArticleDTO>> GetUnusedNewsArticlesForTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
        {
            if (topicId == Guid.Empty)
                throw new ArgumentOutOfRangeException(
                    nameof(topicId), "TopicId cannot be empty.");

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

            // 3) filter out used IDs
            var usedIds = new HashSet<Guid>(slotIndex.Values);
            var unused = allArticles
                .Where(a => !usedIds.Contains(a.Id))
                .ToList();

            return unused;
        }
        public async Task<ContentsDailyPageResponse> GetDailyLivingLandingAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting GetDailyLivingLandingAsync");

            // 1) Fetch slots 1–9
            var slots = await GetAllSlotsAsync(ct) ?? new List<DailyTopSectionSlot>();
            _logger.LogDebug("Retrieved {Count} slots from state store", slots.Count);
            if (!slots.Any())
                _logger.LogWarning("No slots found in state; landing page will be empty");

            // 2) Top Story (slot 1, Article)
            var topStoryItems = new List<ContentPost>();
            var slot1 = slots.FirstOrDefault(s => s.SlotNumber == 1 && s.ContentType == DailyContentType.Article);
            if (slot1 == null)
            {
                _logger.LogWarning("Slot 1 (Top Story) is empty or not an Article");
            }
            else if (slot1.RelatedContentId == Guid.Empty)
            {
                _logger.LogWarning("Slot 1 has no RelatedContentId");
            }
            else
            {
                _logger.LogDebug("Loading Article {ArticleId} for Top Story", slot1.RelatedContentId);
                var article = await _news.GetArticleByIdAsync(slot1.RelatedContentId, ct);
                if (article == null)
                {
                    _logger.LogWarning("Article {ArticleId} not found or unpublished", slot1.RelatedContentId);
                }
                else
                {
                    topStoryItems.Add(new ContentPost
                    {
                        PageName = DrupalContentConstants.QlnContentsDaily,
                        QueueLabel = "Top Story",
                        NodeType = "Post",
                        Nid = article.Id.ToString(),
                        DateCreated = article.CreatedAt.ToString("o"),
                        ImageUrl = article.CoverImageUrl,
                        UserName = article.authorName,
                        Title = article.Title,
                        Slug = article.Slug,
                        Category = article.Categories?.FirstOrDefault()?.CategoryId.ToString() ?? string.Empty
                    });
                    _logger.LogInformation("Top Story loaded: {Title}", article.Title);
                }
            }

            // 3) Top Stories (slots 3–5, Article)
            var topStoriesItems = new List<ContentPost>();
            foreach (var slot in slots.Where(s => s.SlotNumber is >= 3 and <= 5))
            {
                if (slot.ContentType != DailyContentType.Article)
                {
                    _logger.LogWarning("Slot {Slot} in TopStories is not an Article; skipping", slot.SlotNumber);
                    continue;
                }

                _logger.LogDebug("Loading Article {ArticleId} for TopStories slot {Slot}", slot.RelatedContentId, slot.SlotNumber);
                var art = await _news.GetArticleByIdAsync(slot.RelatedContentId, ct);
                if (art == null)
                {
                    _logger.LogWarning("Article {ArticleId} not found for slot {Slot}", slot.RelatedContentId, slot.SlotNumber);
                    continue;
                }

                topStoriesItems.Add(new ContentPost
                {
                    PageName = DrupalContentConstants.QlnContentsDaily,
                    QueueLabel = "Top Stories",
                    NodeType = "Post",
                    Nid = art.Id.ToString(),
                    DateCreated = art.CreatedAt.ToString("o"),
                    ImageUrl = art.CoverImageUrl,
                    UserName = art.authorName,
                    Title = art.Title,
                    Slug = art.Slug,
                    Category = art.Categories?.FirstOrDefault()?.CategoryId.ToString() ?? string.Empty
                });
            }
            _logger.LogInformation("Top Stories loaded: {Count} items", topStoriesItems.Count);

            // 4) Highlighted Event (slot 2, Event)
            var highlightedEventItems = new List<ContentEvent>();
            var slot2 = slots.FirstOrDefault(s => s.SlotNumber == 2 && s.ContentType == DailyContentType.Event);
            if (slot2 == null)
            {
                _logger.LogWarning("Slot 2 (Highlighted Event) is empty or not an Event");
            }
            else
            {
                _logger.LogDebug("Loading Event {EventId} for HighlightedEvent", slot2.RelatedContentId);
                var ev = await _events.GetEventById(slot2.RelatedContentId, ct);
                if (ev == null)
                {
                    _logger.LogWarning("Event {EventId} not found or inactive", slot2.RelatedContentId);
                }
                else
                {
                    highlightedEventItems.Add(new ContentEvent
                    {
                        PageName = DrupalContentConstants.QlnContentsDaily,
                        QueueLabel = "Highlighted Event",
                        NodeType = "Event",
                        Nid = ev.Id.ToString(),
                        DateCreated = ev.CreatedAt.ToString("o"),
                        ImageUrl = ev.CoverImage,
                        UserName = ev.CreatedBy,
                        Title = ev.EventTitle,
                        Slug = ev.Slug,
                        Category = ev.CategoryName,
                        EventVenue = ev.Venue,
                        EventStart = ev.EventSchedule?.StartDate.ToString("o") ?? string.Empty,
                        EventEnd = ev.EventSchedule?.EndDate.ToString("o") ?? string.Empty,
                        EventLat = ev.Latitude,
                        EventLong = ev.Longitude,
                        EventLocation = ev.LocationId.ToString()
                    });
                    _logger.LogInformation("Highlighted Event loaded: {Title}", ev.EventTitle);
                }
            }

            // 5) Featured Events (all flagged featured)
            var featuredEventItems = new List<ContentEvent>();
            _logger.LogDebug("Loading all featured events");
            var featured = await _events.GetAllIsFeaturedEvents(true, ct) ?? new List<V2Events>();
            foreach (var ev in featured)
            {
                featuredEventItems.Add(new ContentEvent
                {
                    PageName = DrupalContentConstants.QlnContentsDaily,
                    QueueLabel = "Featured Events",
                    NodeType = "Event",
                    Nid = ev.Id.ToString(),
                    DateCreated = ev.CreatedAt.ToString("o"),
                    ImageUrl = ev.CoverImage,
                    UserName = ev.CreatedBy,
                    Title = ev.EventTitle,
                    Slug = ev.Slug,
                    Category = ev.CategoryName,
                    EventVenue = ev.Venue,
                    EventStart = ev.EventSchedule?.StartDate.ToString("o") ?? string.Empty,
                    EventEnd = ev.EventSchedule?.EndDate.ToString("o") ?? string.Empty,
                    EventLat = ev.Latitude,
                    EventLong = ev.Longitude,
                    EventLocation = ev.LocationId.ToString()
                });
            }
            _logger.LogInformation("Featured Events loaded: {Count}", featuredEventItems.Count);

            // 6) More Articles (slots 6–9)
            var moreArticlesItems = new List<ContentEvent>();
            foreach (var slot in slots.Where(s => s.SlotNumber is >= 6 and <= 9))
            {
                if (slot.ContentType != DailyContentType.Article)
                {
                    _logger.LogWarning("Slot {Slot} in MoreArticles is not an Article; skipping", slot.SlotNumber);
                    continue;
                }

                _logger.LogDebug("Loading Article {ArticleId} for MoreArticles slot {Slot}", slot.RelatedContentId, slot.SlotNumber);
                var art = await _news.GetArticleByIdAsync(slot.RelatedContentId, ct);
                if (art == null)
                {
                    _logger.LogWarning("Article {ArticleId} not found for slot {Slot}", slot.RelatedContentId, slot.SlotNumber);
                    continue;
                }

                moreArticlesItems.Add(new ContentEvent
                {
                    PageName = DrupalContentConstants.QlnContentsDaily,
                    QueueLabel = "More Articles",
                    NodeType = "Post",
                    Nid = art.Id.ToString(),
                    DateCreated = art.CreatedAt.ToString("o"),
                    ImageUrl = art.CoverImageUrl,
                    UserName = art.authorName,
                    Title = art.Title,
                    Slug = art.Slug,
                    Category = art.Categories?.FirstOrDefault()?.CategoryId.ToString() ?? string.Empty
                });
            }
            _logger.LogInformation("More Articles loaded: {Count}", moreArticlesItems.Count);

            // 7) Dynamic Topics (one queue per published topic)
            var allTopics = (await GetAllDailyTopicsAsync(ct))
                            .Where(t => t.IsPublished)
                            .ToList();
            var topicQueues = new List<BaseQueueResponse<ContentEvent>>();
            _logger.LogDebug("Building queues for {Count} published topics", allTopics.Count);

            foreach (var topic in allTopics)
            {
                var items = new List<ContentEvent>();
                var topicSlots = await GetSlotsByTopicAsync(topic.Id, ct) ?? new List<DailyTopicContent>();
                _logger.LogDebug("Topic '{Topic}' has {Count} slots", topic.TopicName, topicSlots.Count);

                foreach (var slot in topicSlots)
                {
                    switch (slot.ContentType)
                    {
                        case DailyContentType.Article:
                            _logger.LogDebug("Topic {Topic}: loading Article {Id}", topic.TopicName, slot.RelatedContentId);
                            var a = await _news.GetArticleByIdAsync(slot.RelatedContentId, ct);
                            if (a != null)
                                items.Add(new ContentEvent
                                {
                                    PageName = DrupalContentConstants.QlnContentsDaily,
                                    Nid = a.Id.ToString(),
                                    Title = a.Title,
                                    Category = a.Categories?.FirstOrDefault()?.CategoryId.ToString() ?? string.Empty,
                                    DateCreated = a.CreatedAt.ToString("o"),
                                    ImageUrl = a.CoverImageUrl,
                                    Slug = a.Slug,
                                    NodeType = "Post"
                                });
                            else
                                _logger.LogWarning("Topic {Topic}: Article {Id} not found", topic.TopicName, slot.RelatedContentId);
                            break;

                        case DailyContentType.Event:
                            _logger.LogDebug("Topic {Topic}: loading Event {Id}", topic.TopicName, slot.RelatedContentId);
                            var e2 = await _events.GetEventById(slot.RelatedContentId, ct);
                            if (e2 != null)
                                items.Add(new ContentEvent
                                {
                                    PageName = DrupalContentConstants.QlnContentsDaily,
                                    Nid = e2.Id.ToString(),
                                    Title = e2.EventTitle,
                                    Category = e2.CategoryName,
                                    DateCreated = e2.EventSchedule?.StartDate.ToString("o") ?? string.Empty,
                                    EventStart = e2.EventSchedule?.StartDate.ToString("o") ?? string.Empty,
                                    EventEnd = e2.EventSchedule?.EndDate.ToString("o") ?? string.Empty,
                                    ImageUrl = e2.CoverImage,
                                    EventVenue = e2.Venue,
                                    EventLat = e2.Latitude,
                                    EventLong = e2.Longitude,
                                    Slug = e2.Slug,
                                    NodeType = "Event"
                                });
                            else
                                _logger.LogWarning("Topic {Topic}: Event {Id} not found", topic.TopicName, slot.RelatedContentId);
                            break;

                        case DailyContentType.Video:
                            _logger.LogDebug("Topic {Topic}: adding Video slot {Slot}", topic.TopicName, slot.SlotNumber);
                            items.Add(new ContentEvent
                            {
                                PageName = DrupalContentConstants.QlnContentsDaily,
                                Nid = slot.RelatedContentId.ToString(),
                                Title = slot.Title,
                                Category = slot.Category,
                                DateCreated = slot.PublishedDate.ToString("o"),
                                ImageUrl = slot.ContentUrl,
                                Slug = slot.ContentUrl,
                                NodeType = "Video"
                            });
                            break;
                    }
                }

                topicQueues.Add(new BaseQueueResponse<ContentEvent>
                {
                    QueueLabel = topic.TopicName,
                    Items = items
                });

                _logger.LogInformation("Topic '{Topic}' queue built with {Count} items", topic.TopicName, items.Count);
            }

            // Helper to avoid out‐of‐range on topicQueues
            BaseQueueResponse<ContentEvent> GetOrEmpty(int idx) =>
                idx < topicQueues.Count
                    ? topicQueues[idx]
                    : new BaseQueueResponse<ContentEvent> { QueueLabel = string.Empty, Items = new List<ContentEvent>() };

            // 8) Assemble everything into the DTO
            var contents = new ContentsDaily
            {
                DailyTopStory = new BaseQueueResponse<ContentPost> { QueueLabel = "Top Story", Items = topStoryItems },
                DailyTopStories = new BaseQueueResponse<ContentPost> { QueueLabel = "Top Stories", Items = topStoriesItems },
                DailyEvent = new BaseQueueResponse<ContentEvent> { QueueLabel = "Highlighted Event", Items = highlightedEventItems },
                DailyFeaturedEvents = new BaseQueueResponse<ContentEvent> { QueueLabel = "Featured Events", Items = featuredEventItems },
                DailyMoreArticles = new BaseQueueResponse<ContentEvent> { QueueLabel = "More Articles", Items = moreArticlesItems },
                DailyTopics1 = GetOrEmpty(0),
                DailyTopics2 = GetOrEmpty(1),
                DailyTopics3 = GetOrEmpty(2),
                DailyTopics4 = GetOrEmpty(3),
                DailyTopics5 = GetOrEmpty(4),
            };

            _logger.LogInformation("GetDailyLivingLandingAsync completed successfully");
            return new ContentsDailyPageResponse { ContentsDaily = contents };
        }

    }
}
