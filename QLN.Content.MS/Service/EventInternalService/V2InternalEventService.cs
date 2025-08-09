using Amazon.Runtime.Internal.Util;
using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.Utilities;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.EventInternalService
{
    public class V2InternalEventService : IV2EventService
    {
        private readonly DaprClient _dapr;
        private const string DailyStore = ConstantValues.V2Content.ContentStoreName;
        private readonly ILogger<IV2EventService> _log;
        public V2InternalEventService(DaprClient dapr, ILogger<IV2EventService> log)
        {
            _dapr = dapr;
            _log = log;
        }
        public async Task<string> CreateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.EventTitle))
                    throw new ArgumentException("Event title must not be empty.");
                if (dto.EventType == V2EventType.FeePrice)
                {
                    if (dto.Price == null)
                        throw new ArgumentException("Price must be provided when the EventType is 'Fees'.");
                }
                else
                {
                    if (dto.Price != null)
                        throw new ArgumentException("Price must not be entered for 'Free Access' or 'Open Registration' events.");
                }
                string categoryName = string.Empty;

                var categoryKeys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventCategoryIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (categoryKeys.Contains(dto.CategoryId.ToString()))
                {
                    var selectedCategory = await _dapr.GetStateAsync<EventsCategory>(
                        ConstantValues.V2Content.ContentStoreName,
                        dto.CategoryId.ToString(),
                        cancellationToken: cancellationToken
                    );

                    if (selectedCategory != null)
                        categoryName = selectedCategory.CategoryName;
                }

                ValidateEventSchedule(dto.EventSchedule);
                var id = Guid.NewGuid();
                var slug = ProcessingHelpers.GenerateSlug(dto.EventTitle);
                var entity = new V2Events
                {
                    Id = id,
                    Slug = slug,
                    CategoryId = dto.CategoryId,
                    CategoryName = categoryName,
                    EventTitle = dto.EventTitle,
                    EventType = dto.EventType,
                    Price = dto.Price,
                    EventSchedule = dto.EventSchedule,
                    LocationId = dto.LocationId,
                    Location = dto.Location,
                    Venue = dto.Venue,
                    Longitude = dto.Longitude,
                    Latitude = dto.Latitude,
                    RedirectionLink = dto.RedirectionLink,
                    EventDescription = dto.EventDescription,
                    CoverImage = dto.CoverImage,
                    IsFeatured = false,
                    FeaturedSlot = dto.FeaturedSlot,
                    Status = dto.Status,
                    PublishedDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    entity,
                    cancellationToken: cancellationToken
                );

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(id.ToString()))
                {
                    keys.Add(id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.EventIndexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }

                var upsertRequest = await IndexEventToAzureSearch(entity, cancellationToken);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentEventsIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                return "Event created successfully.";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating event", ex);
            }
        }
        private async Task<string> HandleEventSlotShift(int desiredSlot, V2Events newEvent, CancellationToken cancellationToken)
        {
            const int MaxSlot = 6;
            string storeName = ConstantValues.V2Content.ContentStoreName;

            string desiredSlotKey = $"event-slot-{desiredSlot}";
            var existingInDesiredSlot = await _dapr.GetStateAsync<V2Events>(storeName, desiredSlotKey, cancellationToken: cancellationToken);

            if (existingInDesiredSlot != null && existingInDesiredSlot.Id != newEvent.Id)
            {
                existingInDesiredSlot.IsFeatured = false;
                existingInDesiredSlot.FeaturedSlot = new();
                await _dapr.SaveStateAsync(storeName, existingInDesiredSlot.Id.ToString(), existingInDesiredSlot, cancellationToken: cancellationToken);
            }
            newEvent.FeaturedSlot.Id = desiredSlot;
            newEvent.IsFeatured = true;
            await _dapr.SaveStateAsync(storeName, desiredSlotKey, newEvent, cancellationToken: cancellationToken);
            await _dapr.SaveStateAsync(storeName, newEvent.Id.ToString(), newEvent, cancellationToken: cancellationToken);

            return $"Event placed in slot {desiredSlot} successfully.";
        }
        private void ValidateEventSchedule(EventSchedule schedule)
        {
            if (schedule == null)
                throw new ArgumentException("EventSchedule is required.");

            if (schedule.StartDate == default || schedule.EndDate == default)
                throw new ArgumentException("StartDate and EndDate must be provided.");

            if (schedule.StartDate > schedule.EndDate)
                throw new ArgumentException("StartDate must not be later than EndDate.");

            if (!string.IsNullOrWhiteSpace(schedule.GeneralTextTime) && schedule.GeneralTextTime.Length > 50)
                throw new ArgumentException("GeneralTextTime must not exceed 50 characters.");

            if (schedule.TimeSlots != null)
            {
                foreach (var slot in schedule.TimeSlots)
                {
                    if (!string.IsNullOrWhiteSpace(slot.TextTime) && slot.TextTime.Length > 50)
                        throw new ArgumentException("Each TimeSlot.TextTime must not exceed 50 characters.");
                }
            }
        }
        //private string GenerateSlug(string title)
        //{
        //    if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        //    var slug = title.ToLowerInvariant().Trim();
        //    slug = Regex.Replace(slug, @"[\s_]+", "-");
        //    slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        //    slug = Regex.Replace(slug, @"-+", "-");
        //    slug = slug.Trim('-');
        //    return slug;
        //}
        public async Task<List<V2Events>> GetAllEvents(CancellationToken cancellationToken)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();
            if (keys.Count == 0)
                return new List<V2Events>();
            var items = await _dapr.GetBulkStateAsync(
                ConstantValues.V2Content.ContentStoreName,
                keys,
                parallelism: null,
                cancellationToken: cancellationToken
            );

            var events = items
                .Select(i => JsonSerializer.Deserialize<V2Events>(i.Value, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }))
                .Where(e => e != null && e.IsActive == true)
                .ToList();

            return events ?? new List<V2Events>();
        }
        public async Task<V2Events?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken) ?? new();

                if (keys.Count == 0)
                    return null;

                var items = await _dapr.GetBulkStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    keys,
                    parallelism: null,
                    cancellationToken: cancellationToken);

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Value))
                        continue;

                    var ev = JsonSerializer.Deserialize<V2Events>(item.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (ev is not null && ev.IsActive && ev.Id == id)
                    {
                        return ev;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<string> UpdateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.EventTitle))
                    throw new ArgumentException("Event title must not be empty.");

                if (dto.Id == Guid.Empty)
                    throw new ArgumentException("Event ID is required for update.");

                if (dto.EventType == V2EventType.FeePrice)
                {
                    if (dto.Price == null)
                        throw new ArgumentException("Price must be provided when the EventType is 'Fees'.");
                }
                else
                {
                    if (dto.Price != null)
                        throw new ArgumentException("Price must not be entered for 'Free Access' or 'Open Registration' events.");
                }

                string categoryName = string.Empty;
                var categoryKeys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventCategoryIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (categoryKeys.Contains(dto.CategoryId.ToString()))
                {
                    var selectedCategory = await _dapr.GetStateAsync<EventsCategory>(
                        ConstantValues.V2Content.ContentStoreName,
                        dto.CategoryId.ToString(),
                        cancellationToken: cancellationToken
                    );

                    if (selectedCategory != null)
                        categoryName = selectedCategory.CategoryName;
                }

                ValidateEventSchedule(dto.EventSchedule);

                var existing = await _dapr.GetStateAsync<V2Events>(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    cancellationToken: cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException($"Event with ID {dto.Id} not found.");
                var endDate = dto.EventSchedule?.EndDate.ToDateTime(TimeOnly.MinValue).Date;

                if (endDate < DateTime.UtcNow.Date)
                {
                    dto.Status = EventStatus.Expired;
                    dto.PublishedDate = null;
                }
                else
                {
                    if (existing.Status == EventStatus.Expired)
                    {
                        dto.Status = EventStatus.Published;
                        dto.PublishedDate = DateTime.UtcNow;
                    }
                    else
                    {
                        dto.Status = dto.Status;
                        dto.PublishedDate = dto.Status == EventStatus.Published ? DateTime.UtcNow : null;
                    }
                }
                if (existing.Status == EventStatus.Published && dto.Status == EventStatus.UnPublished)
                {
                    if (existing.IsFeatured == true)
                    {
                        throw new InvalidOperationException($"Cannot unpublish event {dto.Id}: It is currently featured.");
                    }
                    existing.PublishedDate = null;
                    var topSlotTasks = Enumerable.Range(1, 9)
                        .Select(i => _dapr.GetStateAsync<DailyTopSectionSlot>(
                            DailyStore,
                            $"daily-slot-{i}",
                            cancellationToken: cancellationToken))
                        .ToArray();

                    var topSlots = (await Task.WhenAll(topSlotTasks))
                        .Where(s => s != null)
                        .ToList();

                    var usedInTop = topSlots.FirstOrDefault(s =>
                        s.ContentType == DailyContentType.Event &&
                        s.RelatedContentId == dto.Id);

                    if (usedInTop != null)
                    {
                        _log.LogWarning("Attempt to unpublish Event {EventId} denied: Found in Daily Top Slot #{Slot}",
                            dto.Id, usedInTop.SlotNumber);

                        throw new InvalidOperationException($"Cannot unpublish event {dto.Id}: It is used in Daily Top Section slot #{usedInTop.SlotNumber}");
                    }

                    var topicIds = await GetAllDailyTopicIdsAsync(cancellationToken);
                    foreach (var topicId in topicIds)
                    {
                        var topicSlots = await GetSlotsByTopicIdAsync(topicId, cancellationToken);
                        var usedInTopic = topicSlots.FirstOrDefault(ts =>
                            ts.ContentType == DailyContentType.Event &&
                            ts.RelatedContentId == dto.Id);

                        if (usedInTopic != null)
                        {
                            _log.LogWarning("Attempt to unpublish Event {EventId} denied: Found in Topic Slot #{Slot} of TopicId {TopicId}",
                                dto.Id, usedInTopic.SlotNumber, topicId);

                            throw new InvalidOperationException($"Cannot unpublish event {dto.Id}: It is used in Topic slot #{usedInTopic.SlotNumber}");
                        }
                    }
                }

                var slug = ProcessingHelpers.GenerateSlug(dto.EventTitle);

                var updated = new V2Events
                {
                    Id = dto.Id,
                    Slug = dto.Slug,
                    CategoryId = dto.CategoryId,
                    CategoryName = categoryName,
                    EventTitle = dto.EventTitle,
                    EventType = dto.EventType,
                    Price = dto.Price,
                    EventSchedule = dto.EventSchedule,
                    Location = dto.Location,
                    LocationId = dto.LocationId,
                    Venue = dto.Venue,
                    Longitude = dto.Longitude,
                    Latitude = dto.Latitude,
                    RedirectionLink = dto.RedirectionLink,
                    EventDescription = dto.EventDescription,
                    CoverImage = dto.CoverImage,
                    IsFeatured = false,
                    FeaturedSlot = dto.FeaturedSlot,
                    Status = dto.Status,
                    PublishedDate = dto.Status == EventStatus.UnPublished || dto.Status == EventStatus.Expired
                    ? null
                    : dto.Status == EventStatus.Published
                        ? DateTime.UtcNow
                        : existing.PublishedDate,
                    IsActive = true,
                    CreatedBy = existing.CreatedBy,
                    CreatedAt = existing.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    updated,
                    cancellationToken: cancellationToken);

                var upsertRequest = await IndexEventToAzureSearch(updated, cancellationToken);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ContentEventsIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }

                _log.LogInformation("Event {EventId} updated successfully by user {UserId}", dto.Id, userId);

                return "Event updated successfully";
            }
            catch (KeyNotFoundException ex)
            {
                _log.LogError(ex, "Event not found while updating: {EventId}", dto.Id);
                throw new KeyNotFoundException($"Event with ID '{dto.Id}' not found.", ex);
            }
            catch (ArgumentException ex)
            {
                _log.LogError(ex, "Validation error while updating event {EventId}", dto.Id);
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (InvalidOperationException ex)
            {
                _log.LogWarning(ex, "Update blocked: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unhandled error while updating event {EventId}", dto.Id);
                throw new Exception("Error updating events", ex);
            }
        }
        private async Task<List<Guid>> GetAllDailyTopicIdsAsync(CancellationToken ct)
        {
            try
            {
                var topics = await _dapr.GetStateAsync<List<Guid>>(
                    DailyStore,
                    "daily-topics-index",
                    cancellationToken: ct)
                    ?? new List<Guid>();
                return topics;
            }
            catch (Exception ex)
            {
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }
        }
        private async Task<List<DailyTopicContent>> GetSlotsByTopicIdAsync(Guid topicId, CancellationToken ct)
        {
            try
            {
                var key = $"daily-topic-{topicId}-slots";
                var slots = await _dapr.GetStateAsync<List<DailyTopicContent>>(
                    DailyStore,
                    key,
                    cancellationToken: ct)
                    ?? new List<DailyTopicContent>();
                return slots;
            }
            catch (Exception ex)
            {
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }
        }
        public async Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var topSlotTasks = Enumerable.Range(1, 9)
                    .Select(i => _dapr.GetStateAsync<DailyTopSectionSlot>(
                        DailyStore,
                        $"daily-slot-{i}",
                        cancellationToken: cancellationToken))
                    .ToArray();

                var topSlots = (await Task.WhenAll(topSlotTasks))
                    .Where(s => s != null)
                    .ToList();

                var usedInTop = topSlots
                    .FirstOrDefault(s => s.ContentType == DailyContentType.Event
                                      && s.RelatedContentId == id);

                if (usedInTop != null)
                    throw new InvalidOperationException(
                        $"Cannot delete event {id}: it’s used in Daily Top Section slot #{usedInTop.SlotNumber}");
            }
            catch (DaprServiceException)
            {
                throw;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new DaprServiceException(
                    statusCode: StatusCodes.Status502BadGateway,
                    responseBody: ex.Message);
            }

            List<Guid> topicIds = await GetAllDailyTopicIdsAsync(cancellationToken);
            foreach (var topicId in topicIds)
            {
                var topicSlots = await GetSlotsByTopicIdAsync(topicId, cancellationToken);
                var usedInTopic = topicSlots
                    .FirstOrDefault(ts => ts.ContentType == DailyContentType.Event
                                       && ts.RelatedContentId == id);

                if (usedInTopic != null)
                    throw new InvalidOperationException(
                        $"Cannot delete event {id}: it’s used in Topic slot #{usedInTopic.SlotNumber}");
            }

            var existing = await _dapr.GetStateAsync<V2Events>(
                V2Content.ContentStoreName,
                id.ToString(),
                cancellationToken: cancellationToken);

            if (existing == null)
                throw new KeyNotFoundException($"Event with ID '{id}' not found.");

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(
                V2Content.ContentStoreName,
                id.ToString(),
                existing,
                new StateOptions { Consistency = ConsistencyMode.Strong },
                cancellationToken: cancellationToken);

            var upsertRequest = await IndexEventToAzureSearch(existing, cancellationToken);
            if (upsertRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ContentEventsIndex,
                    UpsertRequest = upsertRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }

            return "Event Soft Deleted Successfully";
        }
        public async Task<string> CreateCategory(EventsCategory category, CancellationToken cancellationToken)
        {
            try
            {
                if (category == null || string.IsNullOrWhiteSpace(category.CategoryName))
                {
                    throw new InvalidDataException("Category name is required."); 
                }
                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    category.Id.ToString(),
                    category,
                    cancellationToken: cancellationToken
                );

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventCategoryIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(category.Id.ToString()))
                {
                    keys.Add(category.Id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.EventCategoryIndexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }

                return "Category created successfully."; 
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while creating the event category.", ex);
            }
        }
        public async Task<List<EventsCategory>> GetAllCategories(CancellationToken cancellationToken)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.V2Content.ContentStoreName,
                ConstantValues.V2Content.EventCategoryIndexKey,
                cancellationToken: cancellationToken
            ) ?? new List<string>();

            if (keys.Count == 0)
                return new List<EventsCategory>();

            var items = await _dapr.GetBulkStateAsync(
                ConstantValues.V2Content.ContentStoreName,
                keys,
                parallelism: null,
                cancellationToken: cancellationToken
            );

            var categories = items
                .Select(i => JsonSerializer.Deserialize<EventsCategory>(i.Value, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }))
                .Where(e => e != null)
                .ToList();

            return categories ?? new List<EventsCategory>(); 
        }
        public async Task<EventsCategory?> GetEventCategoryById(int id, CancellationToken cancellationToken = default)
        {
            var result = await _dapr.GetStateAsync<EventsCategory>(
                  ConstantValues.V2Content.ContentStoreName,
                  id.ToString(),
                  cancellationToken: cancellationToken);
            if (result == null)
                return null;
            return result;
        }
        public async Task<PagedResponse<V2Events>> GetPagedEvents(GetPagedEventsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.Page <= 0 || request.PerPage <= 0)
                {
                    throw new ArgumentException("Page and PerPage must be greater than zero.");
                }
                var allEvents = new List<V2Events>();
                var eventIds = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken);

                if (eventIds == null || !eventIds.Any())
                {
                    return EmptyResponse(request.Page, request.PerPage);
                }

                var fetchTasks = eventIds.Select(async eventId =>
                {
                    try
                    {
                        var ev = await _dapr.GetStateAsync<V2Events>(
                            ConstantValues.V2Content.ContentStoreName,
                            eventId,
                            cancellationToken: cancellationToken);
                        return ev;
                    }
                    catch
                    {
                        return null;
                    }
                });

                var fetchedEvents = await Task.WhenAll(fetchTasks);
                allEvents = fetchedEvents.Where(e => e != null && e.IsActive).ToList();

                if (!allEvents.Any())
                {
                    return EmptyResponse(request.Page, request.PerPage);
                }

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    allEvents = allEvents
                        .Where(e => !string.IsNullOrEmpty(e.EventTitle) &&
                                    e.EventTitle.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

                foreach (var ev in allEvents)
                {
                    if (ev.EventSchedule.EndDate < today && ev.Status != EventStatus.Expired) 
                    {
                        ev.Status = EventStatus.Expired;
                        ev.PublishedDate = null;
                        await _dapr.SaveStateAsync<V2Events>(V2Content.ContentStoreName, ev.Id.ToString(), ev, cancellationToken: cancellationToken);
                    }
                }

                if (request.Status.HasValue)
                {
                    allEvents = allEvents
                        .Where(e => e.Status == request.Status.Value)
                        .ToList();
                }

                if (request.CategoryId.HasValue)
                {
                    allEvents = allEvents
                        .Where(e => e.CategoryId == request.CategoryId.Value)
                        .ToList();
                }
                if (request.FromDate.HasValue || request.ToDate.HasValue)
                {
                    allEvents = allEvents
                        .Where(e =>
                        {
                            var eventStart = e.EventSchedule.StartDate;
                            var eventEnd = e.EventSchedule.EndDate;

                            if (request.FromDate.HasValue && request.ToDate.HasValue)
                                return eventStart >= request.FromDate && eventEnd <= request.ToDate;

                            if (request.FromDate.HasValue)
                                return eventEnd >= request.FromDate;

                            if (request.ToDate.HasValue)
                                return eventStart <= request.ToDate;

                            return true;
                        })
                        .ToList();
                }
                if (!string.IsNullOrWhiteSpace(request.FilterType))
                {
                    switch (request.FilterType.ToLowerInvariant())
                    {
                        case "featured":
                            allEvents = allEvents
                                .Where(e => e.IsFeatured)
                                .ToList();
                            break;

                        case "thisweek":
                            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                            var endOfWeek = startOfWeek.AddDays(6);

                            allEvents = allEvents
                                .Where(e => e.EventSchedule.StartDate >= startOfWeek &&
                                            e.EventSchedule.StartDate <= endOfWeek)
                                .ToList();
                            break;

                        case "upcoming":
                            allEvents = allEvents
                                .Where(e => e.EventSchedule.StartDate >= today)
                                .ToList();
                            break;
                    }
                }
                if (request.LocationId is { Count: > 0 })
                {
                    allEvents = allEvents
                        .Where(e => e.LocationId != null && request.LocationId.Contains(e.LocationId.Value))
                        .ToList();
                }
                if (request.FreeOnly == true)
                {
                    allEvents = allEvents
                        .Where(e => e.EventType == V2EventType.FreeAcess || e.EventType == V2EventType.OpenRegistrations)
                        .ToList();
                }

                if (!allEvents.Any())
                    return EmptyResponse(request.Page, request.PerPage);

                request.SortOrder = string.IsNullOrWhiteSpace(request.SortOrder) ? "asc" : request.SortOrder.ToLowerInvariant();
                if (request.FeaturedFirst == true)
                {
                    allEvents = request.SortOrder switch
                    {
                        "desc" => allEvents
                            .OrderByDescending(e => e.IsFeatured)
                            .ThenByDescending(e => e.CreatedAt)
                            .ToList(),
                        _ => allEvents
                            .OrderByDescending(e => e.IsFeatured)
                            .ThenBy(e => e.CreatedAt)
                            .ToList(),
                    };
                }
                else
                {
                    allEvents = request.SortOrder switch
                    {
                        "desc" => allEvents.OrderByDescending(e => e.CreatedAt).ToList(),
                        _ => allEvents.OrderBy(e => e.CreatedAt).ToList(),
                    };
                }
                int currentPage = Math.Max(1, request.Page ?? 1);
                int itemsPerPage = Math.Max(1, Math.Min(100, request.PerPage ?? 12));
                int totalCount = allEvents.Count;
                int totalPages = (int)Math.Ceiling((double)totalCount / itemsPerPage);

                if (currentPage > totalPages && totalPages > 0)
                    currentPage = totalPages;

                var paginated = allEvents
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();
                var featuredCount = allEvents.Count(e => e.IsFeatured);
                var featuredInCurrentPage = paginated.Count(e => e.IsFeatured);
                return new PagedResponse<V2Events>
                {
                    Page = currentPage,
                    PerPage = itemsPerPage,
                    TotalCount = totalCount,
                    Items = paginated,
                    FeaturedCount = featuredCount,
                    FeaturedInCurrentPage = featuredInCurrentPage
                };
            }
            catch(ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while retrieving events: {ex.Message}", ex);
            }
        }
        private static PagedResponse<V2Events> EmptyResponse(int? page, int? perPage) => new()
        {
            Page = page ?? 1,
            PerPage = perPage ?? 12,
            TotalCount = 0,
            Items = new List<V2Events>()
        };
        public async Task<List<V2Slot>> GetAllEventSlot(CancellationToken cancellationToken = default)
        {
            try
            {
                var slots = Enum.GetValues(typeof(V2EventSlot))
                  .Cast<V2EventSlot>()
                  .Select(s => new V2Slot
                  {
                      Id = (int)s,
                      Name = s.ToString()
                  })
                  .ToList();
                return await Task.FromResult(slots);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while retrieving event slots", ex);
            }
        }
        public async Task<IEnumerable<V2Events>> GetExpiredEvents(CancellationToken cancellationToken)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Any())
                {
                    return Enumerable.Empty<V2Events>();
                }
                var items = await _dapr.GetBulkStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    keys,
                    parallelism: null,
                    cancellationToken: cancellationToken
                );

                if (items == null || !items.Any())
                {
                    return Enumerable.Empty<V2Events>();
                }
                var allEvents = new List<V2Events>();
                foreach (var item in items)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            var eventData = JsonSerializer.Deserialize<V2Events>(item.Value, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (eventData != null)
                            {
                                allEvents.Add(eventData);
                            }
                        }
                    }
                    catch (JsonException)
                    {
                    }
                }
                var expired = allEvents
                    .Where(e => e.EventSchedule != null && e.EventSchedule.EndDate < today)
                    .ToList();

                return expired;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while retrieving expired events: {ex.Message}", ex);
            }
        }

        public async Task<string> ReorderEventSlotsAsync(EventSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            const int MaxSlot = 6;
            string storeName = V2Content.ContentStoreName;

            if (request.SlotAssignments == null || request.SlotAssignments.Count != MaxSlot)
                throw new InvalidDataException($"Exactly {MaxSlot} slot assignments must be provided.");

            var slotNumbers = request.SlotAssignments.Select(sa => sa.SlotNumber).ToList();
            if (slotNumbers.Distinct().Count() != MaxSlot || slotNumbers.Any(s => s < 1 || s > MaxSlot))
                throw new InvalidDataException("SlotNumber must be unique and between 1 and 6.");

            var loadedEvents = new Dictionary<string, V2Events>();

            foreach (var assignment in request.SlotAssignments)
            {
                if (string.IsNullOrWhiteSpace(assignment.EventId))
                    continue;

                var ev = await _dapr.GetStateAsync<V2Events>(storeName, assignment.EventId, cancellationToken: cancellationToken);
                if (ev == null)
                    throw new InvalidDataException($"Event with ID '{assignment.EventId}' not found.");

                loadedEvents[assignment.EventId] = ev;
            }

            foreach (var assignment in request.SlotAssignments)
            {
                var slotKey = GetEventSlotKey(assignment.SlotNumber);

                if (string.IsNullOrWhiteSpace(assignment.EventId))
                {
                    await _dapr.DeleteStateAsync(storeName, slotKey, cancellationToken: cancellationToken);
                    continue;
                }

                var ev = loadedEvents[assignment.EventId];
                ev.FeaturedSlot.Id = assignment.SlotNumber;

                await _dapr.SaveStateAsync(storeName, slotKey, ev, cancellationToken: cancellationToken);
                await _dapr.SaveStateAsync(storeName, ev.Id.ToString(), ev, cancellationToken: cancellationToken);
            }

            return "Slots updated successfully.";
        }
        private string GetEventSlotKey(int slotId)
        {
            return $"event-slot-{slotId}";
        }
        public async Task<List<V2Events>> GetEventsByStatus(EventStatus status, CancellationToken cancellationToken)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Any())
                {
                    return new List<V2Events>();
                }

                var items = await _dapr.GetBulkStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    keys,
                    parallelism: null,
                    cancellationToken: cancellationToken
                );

                var resultEvents = new List<V2Events>();
                var stateChanges = new List<StateTransactionRequest>();

                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item.Value))
                        continue;

                    try
                    {
                        var eventData = JsonSerializer.Deserialize<V2Events>(item.Value, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (eventData == null || !eventData.IsActive)
                            continue;

                        if (eventData.EventSchedule?.EndDate < today && eventData.Status != EventStatus.Expired)
                        {
                            eventData.Status = EventStatus.Expired;

                            var updatedValue = JsonSerializer.Serialize(eventData);
                            stateChanges.Add(new StateTransactionRequest(
                                item.Key,
                                Encoding.UTF8.GetBytes(updatedValue),
                                StateOperationType.Upsert));
                        }
                        if (eventData.Status == status)
                        {
                            resultEvents.Add(eventData);
                        }
                    }
                    catch (JsonException)
                    {
                    }
                }

                if (stateChanges.Any())
                {
                    await _dapr.ExecuteStateTransactionAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        stateChanges,
                        cancellationToken: cancellationToken
                    );
                }

                return resultEvents;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while retrieving events by status: {ex.Message}", ex);
            }
        }
        public async Task<List<V2Events>> GetEventStatus(EventStatus status, CancellationToken cancellationToken)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Any())
                {
                    return new List<V2Events>();
                }

                var items = await _dapr.GetBulkStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    keys,
                    parallelism: null,
                    cancellationToken: cancellationToken
                );

                var resultEvents = new List<V2Events>();
                var stateChanges = new List<StateTransactionRequest>();

                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item.Value))
                        continue;

                    try
                    {
                        var eventData = JsonSerializer.Deserialize<V2Events>(item.Value, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (eventData == null || !eventData.IsActive || !eventData.IsFeatured)
                            continue;

                        if (eventData.EventSchedule?.EndDate < today && eventData.Status != EventStatus.Expired)
                        {
                            eventData.Status = EventStatus.Expired;

                            var updatedValue = JsonSerializer.Serialize(eventData);
                            stateChanges.Add(new StateTransactionRequest(
                                item.Key,
                                Encoding.UTF8.GetBytes(updatedValue),
                                StateOperationType.Upsert));
                        }
                        if (eventData.Status == status)
                        {
                            resultEvents.Add(eventData);
                        }
                    }
                    catch (JsonException)
                    {
                    }
                }

                if (stateChanges.Any())
                {
                    await _dapr.ExecuteStateTransactionAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        stateChanges,
                        cancellationToken: cancellationToken
                    );
                }

                return resultEvents;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while retrieving events by status: {ex.Message}", ex);
            }
        }
        public async Task UpdateFeaturedEvent(UpdateFeaturedEvent dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingEvent = await _dapr.GetStateAsync<V2Events>(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.EventId.ToString(),
                    cancellationToken: cancellationToken);

                if (existingEvent == null)
                {
                    var eventIds = await _dapr.GetStateAsync<List<string>>(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.EventIndexKey,
                        cancellationToken: cancellationToken);

                    if (eventIds == null || !eventIds.Contains(dto.EventId.ToString()))
                    {
                        throw new InvalidDataException($"Event with ID {dto.EventId} not found.");
                    }

                    existingEvent = await _dapr.GetStateAsync<V2Events>(
                        ConstantValues.V2Content.ContentStoreName,
                        dto.EventId.ToString(),
                        cancellationToken: cancellationToken);

                    if (existingEvent == null)
                    {
                        throw new InvalidDataException($"Event with ID {dto.EventId} not found in storage.");
                    }
                }

                existingEvent.UpdatedBy = dto.UpdatedBy;
                existingEvent.UpdatedAt = DateTime.UtcNow;
                existingEvent.IsFeatured = dto.IsFeatured;
                existingEvent.FeaturedSlot = dto.IsFeatured ? dto.Slot : null;

                if (dto.IsFeatured && dto.Slot != null && dto.Slot.Id >= 1 && dto.Slot.Id <= 6)
                {
                    await HandleEventSlotShift(dto.Slot.Id, existingEvent, cancellationToken);
                }
                else
                {
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        dto.EventId.ToString(),
                        existingEvent,
                        cancellationToken: cancellationToken);
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating featured event: {ex.Message}", ex);
            }
        }
        public async Task<List<V2Events>> GetAllIsFeaturedEvents(bool isFeatured, CancellationToken cancellationToken)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(
                     ConstantValues.V2Content.ContentStoreName,
                     ConstantValues.V2Content.EventIndexKey,
                     cancellationToken: cancellationToken
                 ) ?? new List<string>();
            if (keys.Count == 0)
                return new List<V2Events>();
            var items = await _dapr.GetBulkStateAsync(
                ConstantValues.V2Content.ContentStoreName,
                keys,
                parallelism: null,
                cancellationToken: cancellationToken
            );

            var events = items
                .Select(i => JsonSerializer.Deserialize<V2Events>(i.Value, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }))
                .Where(e => e != null && e.IsActive == true && e.IsFeatured == isFeatured)
                .ToList();

            return events ?? new List<V2Events?>();
        }
        public async Task<string> UnfeatureEvent(Guid id, CancellationToken cancellationToken = default)
        {
            var storeName = ConstantValues.V2Content.ContentStoreName;
            var existing = await _dapr.GetStateAsync<V2Events>(
                storeName,
                id.ToString(),
                cancellationToken: cancellationToken);
            if (existing == null)
                throw new KeyNotFoundException($"Event with ID '{id}' not found.");
            if (existing.IsFeatured && existing.FeaturedSlot?.Id >= 1 && existing.FeaturedSlot.Id <= 6)
            {
                string slotKey = $"event-slot-{existing.FeaturedSlot.Id}";
                var slotEvent = await _dapr.GetStateAsync<V2Events>(
                    storeName,
                    slotKey,
                    cancellationToken: cancellationToken);
                if (slotEvent?.Id == id)
                {
                    await _dapr.DeleteStateAsync(storeName, slotKey, cancellationToken: cancellationToken);
                }
            }
            existing.IsFeatured = false;
            existing.FeaturedSlot = new V2Slot();
            existing.UpdatedAt = DateTime.UtcNow;
            await _dapr.SaveStateAsync(
                storeName,
                id.ToString(),
                existing,
                new StateOptions { Consistency = ConsistencyMode.Strong },
                cancellationToken: cancellationToken);
            return "Event unfeatured and removed from slot successfully";
        }

        private async Task<CommonIndexRequest> IndexEventToAzureSearch(QLN.Common.DTO_s.V2Events dto, CancellationToken cancellationToken)
        {

            var indexDoc = new ContentEventsIndex
            {
                Id = dto.Id.ToString(),
                EventTitle = dto.EventTitle,
                EventType = dto.EventType,
                EventDescription = dto.EventDescription,
                CategoryId = dto.CategoryId,
                CategoryName = dto.CategoryName,
                CoverImage = dto.CoverImage,
                FeaturedSlot = new SlotIndex { 
                    Id = dto.FeaturedSlot.Id, 
                    Name = dto.FeaturedSlot.Name 
                },
                IsFeatured = dto.IsFeatured,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Location = dto.Location,
                LocationId = dto.LocationId,
                Price = dto.Price,
                RedirectionLink = dto.RedirectionLink,
                Status = dto.Status,
                Slug = dto.Slug,
                PublishedDate = dto.PublishedDate,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                EventSchedule = new EventScheduleIndex
                {
                    StartDate = dto.EventSchedule.StartDate.FromDateOnly(),
                    EndDate = dto.EventSchedule.EndDate.FromDateOnly(),
                    GeneralTextTime = dto.EventSchedule.GeneralTextTime,
                    TimeSlotType = dto.EventSchedule.TimeSlotType,
                    TimeSlots = dto.EventSchedule.TimeSlots?.Select(i => new TimeSlotIndex
                    {
                        DayOfWeek = i.DayOfWeek,
                        TextTime = i.TextTime
                    }).ToList()
                }
            };

            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ContentEventsIndex,
                ContentEventsItem = indexDoc
            };
            return indexRequest;

        }
    }
}
