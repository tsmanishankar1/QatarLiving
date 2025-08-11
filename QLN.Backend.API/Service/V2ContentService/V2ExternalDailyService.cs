using Dapr.Client;
using Microsoft.Extensions.Caching.Memory;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.ISearchService;
using System.Text;
using System.Text.Json;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Backend.API.Service.V2ContentService

{

    public class V2ExternalDailyService : IV2ContentDailyService

    {

        private readonly DaprClient _dapr;

        private readonly ILogger<V2ExternalDailyService> _logger;
        private readonly ISearchService _search;

        private const string AppId = V2Content.ContentServiceAppId;

        private const string BaseUrl = "/api/v2/dailyliving/topsection";

        public V2ExternalDailyService(DaprClient dapr, ILogger<V2ExternalDailyService> logger, ISearchService search)

        {

            _dapr = dapr;

            _logger = logger;
            _search = search;

        }
        public async Task<string> UpsertSlotAsync(string userId, DailyTopSectionSlot dto, CancellationToken cancellationToken = default)

        {

            var url = $"{BaseUrl}/{userId}";

            var json = JsonSerializer.Serialize(dto);

            _logger.LogDebug("POST {Url} Payload: {Json}", url, json);

            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, AppId, url);

            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);

            var body = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)

            {

                _logger.LogError(

                    "UpsertSlotAsync → {StatusCode} {Reason}\nResponse: {Body}",

                    (int)res.StatusCode, res.ReasonPhrase, body

                );

                throw new DaprServiceException((int)res.StatusCode, body);

            }

            return JsonSerializer.Deserialize<string>(body, new JsonSerializerOptions

            {

                PropertyNameCaseInsensitive = true

            })!;

        }
        public async Task<List<DailyTopSectionSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)

        {

            var url = BaseUrl;

            _logger.LogDebug("GET {Url}", url);

            var result = await _dapr.InvokeMethodAsync<List<DailyTopSectionSlot>>(

                HttpMethod.Get, AppId, url, cancellationToken

            );

            return result ?? new List<DailyTopSectionSlot>();

        }
        public async Task<List<V2NewsArticleDTO>> GetUnusedDailyTopSectionArticlesAsync(
                    int? page = null, int? pageSize = null, CancellationToken ct = default)
        {
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 12, 1, 100);

            // Read current top-section slots from internal state (no Search in internal)
            var slots = await GetAllSlotsAsync(ct);
            var usedArticleIds = slots
                .Where(s => s.ContentType == DailyContentType.Article && s.RelatedContentId != Guid.Empty)
                .Select(s => s.RelatedContentId.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Build filter: active + any slotId < 15 (published/live lanes)
            var filter = "IsActive eq true and Categories/any(c: c/SlotId lt 15)";
            if (usedArticleIds.Count > 0)
                filter += $" and not search.in(Id, '{string.Join(",", usedArticleIds)}', ',')";

            var res = await _search.SearchRawAsync<ContentNewsIndex>(
                IndexNames.ContentNewsIndex,
                new RawSearchRequest
                {
                    Filter = filter,
                    OrderBy = "PublishedDate desc, CreatedAt desc",
                    Top = ps,
                    Skip = (p - 1) * ps,
                    Text = "*",
                    IncludeTotalCount = false
                },
                ct);

            return res.Items.Select(MapNewsIndexToDto).ToList();
        }

        public async Task<List<V2NewsArticleDTO>> GetUnusedNewsArticlesForTopicAsync(
            Guid topicId, int? page = null, int? pageSize = null, CancellationToken ct = default)
        {
            if (topicId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(topicId));

            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 12, 1, 100);

            // Get slots for topic from internal
            var topicSlots = await GetSlotsByTopicAsync(topicId, ct);
            var usedArticleIds = topicSlots
                .Where(s => s.ContentType == DailyContentType.Article && s.RelatedContentId != Guid.Empty)
                .Select(s => s.RelatedContentId.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Active & not “unpublished” (SlotId != 15)
            var filter = "IsActive eq true and Categories/any(c: c/SlotId ne 15)";
            if (usedArticleIds.Count > 0)
                filter += $" and not search.in(Id, '{string.Join(",", usedArticleIds)}', ',')";

            var res = await _search.SearchRawAsync<ContentNewsIndex>(
                IndexNames.ContentNewsIndex,
                new RawSearchRequest
                {
                    Filter = filter,
                    OrderBy = "PublishedDate desc, CreatedAt desc",
                    Top = ps,
                    Skip = (p - 1) * ps,
                    Text = "*",
                    IncludeTotalCount = false
                },
                ct);

            return res.Items.Select(MapNewsIndexToDto).ToList();
        }
        public async Task<List<DailyTopic>> GetAllDailyTopicsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/dailyliving/dailytopics";
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, appId, path);
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch topics. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                    response.EnsureSuccessStatusCode();
                }

                var topics = JsonSerializer.Deserialize<List<DailyTopic>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return topics ?? new List<DailyTopic>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching DailyTopics");
                throw;
            }
        }
        public async Task<string> CreateContentAsync(string userId, DailyTopicContent dto, CancellationToken ct = default)
        {
            var url = $"/api/v2/dailyliving/topic/contentbyid/{userId}";
            dto.CreatedBy = userId;
            dto.UpdatedBy = userId;
            var payload = JsonSerializer.Serialize(dto);
            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, AppId, url);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new DaprServiceException((int)res.StatusCode, body);
            return JsonSerializer.Deserialize<string>(body)!;
        }
        public async Task<List<DailyTopicContent>> GetSlotsByTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
        {
            var url = $"/api/v2/dailyliving/topic/content?topicId={topicId}";
            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, AppId, url);

            _logger.LogDebug("GET {Url} with TopicId={TopicId}", url, topicId);

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, cancellationToken);
            var body = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "GetSlotsByTopicAsync → {StatusCode} {ReasonPhrase}\n{Body}",
                    (int)res.StatusCode, res.ReasonPhrase, body
                );
                throw new DaprServiceException((int)res.StatusCode, body);
            }

            var list = JsonSerializer.Deserialize<List<DailyTopicContent>>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return list ?? new List<DailyTopicContent>();
        }
        public async Task<string> ReorderSlotsBatchAsync(string userId, DailyTopicSlotReorderRequest dto, CancellationToken ct)
        {
            var url = $"/api/v2/dailyliving/topic/content/reorderbyid/{userId}";
            var payload = JsonSerializer.Serialize(dto);
            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, AppId, url);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new DaprServiceException((int)res.StatusCode, body);
            return JsonSerializer.Deserialize<string>(body)!;
        }
        public async Task<string> DeleteContentAsync(Guid contentId, CancellationToken ct)
        {
            var url = $"/api/v2/dailyliving/topic/content/{contentId}";
            var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Delete, AppId, url);

            using var res = await _dapr.InvokeMethodWithResponseAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new DaprServiceException((int)res.StatusCode, body);
            return JsonSerializer.Deserialize<string>(body)!;
        }
        public async Task<bool> DeleteDailyTopicAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = $"/api/v2/dailyliving/dailytopic/{id}";
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Delete, appId, path);
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to soft delete DailyTopic {Id}. Status: {Status}. Response: {Body}", id, response.StatusCode, responseBody);
                    return false;
                }

                _logger.LogInformation("Successfully soft deleted DailyTopic {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during soft delete of DailyTopic {Id}", id);
                throw;
            }
        }
        public async Task<bool> UpdateDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/dailyliving/dailytopicupdateid";
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, appId, path);
                request.Content = new StringContent(JsonSerializer.Serialize(topic), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to update topic. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update DailyTopic");
                throw;
            }
        }
        public async Task<bool> UpdatePublishStatusAsync(Guid id, bool isPublished, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/dailyliving/publishstatusbyid"; // Internal endpoint without auth
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                // Compose payload as DailyTopic (you can also use a separate DTO if needed)
                var payload = new DailyTopic
                {
                    Id = id,
                    IsPublished = isPublished
                };

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, appId, path);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to update publish status. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                    return false;
                }

                _logger.LogInformation("Successfully updated publish status for topic ID: {Id} to {Status}", id, isPublished);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating publish status for DailyTopic with ID {Id}", id);
                throw;
            }
        }
        public async Task AddDailyTopicAsync(DailyTopic topic, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = "/api/v2/dailyliving/dailytopicById";
                var appId = ConstantValues.V2Content.ContentServiceAppId;

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, appId, path);
                request.Content = new StringContent(JsonSerializer.Serialize(topic), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create topic. Status: {Status}, Content: {Body}", response.StatusCode, responseBody);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create DailyTopic");
                throw;
            }
        }
        public async Task<ContentsDailyPageResponse> GetDailyLivingLandingAsync(CancellationToken ct)
        {
            // Pull layout state from internal…
            var slots = await GetAllSlotsAsync(ct) ?? new();
            var topics = (await GetAllDailyTopicsAsync(ct) ?? new()).Where(t => t.IsPublished).ToList();

            var topStory = new List<ContentPost>();
            var topStories = new List<ContentPost>();
            var highlighted = new List<ContentEvent>();
            var moreArticles = new List<ContentEvent>();

            // S1 → Top Story
            var s1 = slots.FirstOrDefault(s => s.SlotNumber == 1 && s.ContentType == DailyContentType.Article);
            if (s1?.RelatedContentId != Guid.Empty && await LoadNewsAsync(s1.RelatedContentId, ct) is { } n1)
                topStory.Add(MapNewsToPost(n1, "Top Story"));

            // S2 → Highlighted Event
            var s2 = slots.FirstOrDefault(s => s.SlotNumber == 2 && s.ContentType == DailyContentType.Event);
            if (s2?.RelatedContentId != Guid.Empty && await LoadEventAsync(s2.RelatedContentId, ct) is { } e2)
                highlighted.Add(MapEventToEvent(e2, "Highlighted Event"));

            // S3..5 → Top Stories
            foreach (var s in slots.Where(x => x.SlotNumber is >= 3 and <= 5 && x.ContentType == DailyContentType.Article).OrderBy(x => x.SlotNumber))
                if (await LoadNewsAsync(s.RelatedContentId, ct) is { } n)
                    topStories.Add(MapNewsToPost(n, "Top Stories"));

            // S6..9 → More Articles
            foreach (var s in slots.Where(x => x.SlotNumber is >= 6 and <= 9 && x.ContentType == DailyContentType.Article).OrderBy(x => x.SlotNumber))
                if (await LoadNewsAsync(s.RelatedContentId, ct) is { } n)
                    moreArticles.Add(MapNewsToEvent(n, "More Articles"));

            // Dynamic topics
            var topicQueues = new List<BaseQueueResponse<ContentEvent>>();
            foreach (var t in topics)
            {
                var items = new List<ContentEvent>();
                var tSlots = await GetSlotsByTopicAsync(t.Id, ct) ?? new();

                foreach (var s in tSlots.OrderBy(x => x.SlotNumber))
                {
                    if (s.ContentType == DailyContentType.Article && s.RelatedContentId != Guid.Empty)
                    {
                        if (await LoadNewsAsync(s.RelatedContentId, ct) is { } n)
                            items.Add(MapNewsToEvent(n, t.TopicName));
                    }
                    else if (s.ContentType == DailyContentType.Event && s.RelatedContentId != Guid.Empty)
                    {
                        if (await LoadEventAsync(s.RelatedContentId, ct) is { } e)
                            items.Add(MapEventToEvent(e, t.TopicName));
                    }
                    else if (s.ContentType == DailyContentType.Video)
                    {
                        items.Add(new ContentEvent
                        {
                            Id = s.Id,
                            IsActive = true,
                            CreatedAt = s.CreatedAt,
                            UpdatedAt = s.UpdatedAt,
                            PageName = DrupalContentConstants.QlnContentsDaily,
                            QueueLabel = t.TopicName,
                            NodeType = "video",
                            Nid = s.RelatedContentId.ToString(),
                            Title = s.Title,
                            Category = s.Category,
                            DateCreated = s.PublishedDate.ToString("o"),
                            ImageUrl = s.ContentUrl,
                            Slug = s.ContentUrl
                        });
                    }
                }

                topicQueues.Add(new BaseQueueResponse<ContentEvent> { QueueLabel = t.TopicName, Items = items });
            }

            return new ContentsDailyPageResponse
            {
                ContentsDaily = new ContentsDaily
                {
                    DailyTopStory = new BaseQueueResponse<ContentPost> { QueueLabel = "Top Story", Items = topStory },
                    DailyTopStories = new BaseQueueResponse<ContentPost> { QueueLabel = "Top Stories", Items = topStories },
                    DailyEvent = new BaseQueueResponse<ContentEvent> { QueueLabel = "Highlighted Event", Items = highlighted },
                    DailyMoreArticles = new BaseQueueResponse<ContentEvent> { QueueLabel = "More Articles", Items = moreArticles },
                    DailyFeaturedEvents = new BaseQueueResponse<ContentEvent> { QueueLabel = "Featured Events", Items = new() }, // keep simple
                    DailyTopics1 = topicQueues.ElementAtOrDefault(0) ?? new() { QueueLabel = string.Empty, Items = new() },
                    DailyTopics2 = topicQueues.ElementAtOrDefault(1) ?? new() { QueueLabel = string.Empty, Items = new() },
                    DailyTopics3 = topicQueues.ElementAtOrDefault(2) ?? new() { QueueLabel = string.Empty, Items = new() },
                    DailyTopics4 = topicQueues.ElementAtOrDefault(3) ?? new() { QueueLabel = string.Empty, Items = new() },
                    DailyTopics5 = topicQueues.ElementAtOrDefault(4) ?? new() { QueueLabel = string.Empty, Items = new() },
                }
            };
        }

        private async Task<ContentNewsIndex?> LoadNewsAsync(Guid id, CancellationToken ct)
        {
            if (id == Guid.Empty) return null;
            try { return await _search.GetByIdAsync<ContentNewsIndex>(IndexNames.ContentNewsIndex, id.ToString()); }
            catch (Exception ex) { _logger.LogWarning(ex, "News lookup failed for {Id}", id); return null; }
        }

        private async Task<ContentEventsIndex?> LoadEventAsync(Guid id, CancellationToken ct)
        {
            if (id == Guid.Empty) return null;
            try { return await _search.GetByIdAsync<ContentEventsIndex>(IndexNames.ContentEventsIndex, id.ToString()); }
            catch (Exception ex) { _logger.LogWarning(ex, "Event lookup failed for {Id}", id); return null; }
        }

        private static V2NewsArticleDTO MapNewsIndexToDto(ContentNewsIndex i)
        {
            Guid.TryParse(i.Id, out var gid);
            return new V2NewsArticleDTO
            {
                Id = gid,
                Title = i.Title,
                Content = i.Content,
                authorName = i.authorName,
                CoverImageUrl = i.CoverImageUrl,
                Slug = i.Slug,
                UserId = i.UserId,
                WriterTag = i.WriterTag,
                PublishedDate = i.PublishedDate,
                IsActive = i.IsActive,
                CreatedBy = i.CreatedBy,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                UpdatedBy = i.UpdatedBy,
                Categories = i.Categories?.Select(c => new V2ArticleCategory
                {
                    CategoryId = c.CategoryId,
                    SubcategoryId = c.SubcategoryId,
                    SlotId = c.SlotId
                }).ToList() ?? new()
            };
        }

        private static ContentPost MapNewsToPost(ContentNewsIndex i, string label) => new()
        {
            Id = Guid.TryParse(i.Id, out var gid) ? gid : Guid.Empty,
            Nid = i.Id,
            PageName = DrupalContentConstants.QlnContentsDaily,
            QueueLabel = label,
            NodeType = "post",
            Title = i.Title,
            Description = i.Content,
            ImageUrl = i.CoverImageUrl,
            Slug = i.Slug,
            UserName = i.authorName,
            WriterTag = i.WriterTag,
            IsActive = i.IsActive,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
            DateCreated = (i.CreatedAt == default ? DateTime.UtcNow : i.CreatedAt).ToString("o")
        };

        private static ContentEvent MapNewsToEvent(ContentNewsIndex i, string label) => new()
        {
            Id = Guid.TryParse(i.Id, out var gid) ? gid : Guid.Empty,
            Nid = i.Id,
            PageName = DrupalContentConstants.QlnContentsDaily,
            QueueLabel = label,
            NodeType = "post",
            Title = i.Title,
            Description = i.Content,
            ImageUrl = i.CoverImageUrl,
            Slug = i.Slug,
            UserName = i.authorName,
            IsActive = i.IsActive,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
            DateCreated = (i.CreatedAt == default ? DateTime.UtcNow : i.CreatedAt).ToString("o")
        };

        private static ContentEvent MapEventToEvent(ContentEventsIndex e, string label) => new()
        {
            Id = Guid.TryParse(e.Id, out var gid) ? gid : Guid.Empty,
            Nid = e.Id,
            PageName = DrupalContentConstants.QlnContentsDaily,
            QueueLabel = label,
            NodeType = "event",
            Title = e.EventTitle,
            Description = e.EventDescription,
            ImageUrl = e.CoverImage,
            Slug = e.Slug,
            UserName = e.CreatedBy,
            EventCategory = e.CategoryName,
            EventVenue = e.Venue,
            EventStart = e.EventSchedule?.StartDate.ToString("o") ?? "",
            EventEnd = e.EventSchedule?.EndDate.ToString("o") ?? "",
            EventLat = e.Latitude,
            EventLong = e.Longitude,
            EventLocation = e.Location,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            DateCreated = (e.CreatedAt == default ? DateTime.UtcNow : e.CreatedAt).ToString("o")
        };
    }

}

