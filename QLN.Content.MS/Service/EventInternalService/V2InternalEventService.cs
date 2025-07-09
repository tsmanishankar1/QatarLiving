using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Linq;
using System.Text;
using System.Text.Json;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.EventInternalService
{
    public class V2InternalEventService : IV2EventService
    {
        private readonly DaprClient _dapr;
        public V2InternalEventService(DaprClient dapr)
        {
            _dapr = dapr;
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
                ValidateEventSchedule(dto.EventSchedule);
                var id = Guid.NewGuid();
                var slug = GenerateSlug(dto.EventTitle);
                var entity = new V2Events
                {
                    Id = id,
                    Slug = slug,
                    CategoryId = dto.CategoryId,
                    CategoryName = dto.CategoryName ?? string.Empty,
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

            if (existingInDesiredSlot == null)
            {
                newEvent.FeaturedSlot.Id = desiredSlot;
                newEvent.IsFeatured = true;

                await _dapr.SaveStateAsync(storeName, desiredSlotKey, newEvent, cancellationToken: cancellationToken);
                await _dapr.SaveStateAsync(storeName, newEvent.Id.ToString(), newEvent, cancellationToken: cancellationToken);
                return $"Event placed in slot {desiredSlot} successfully.";
            }
            int emptySlot = -1;
            for (int i = desiredSlot + 1; i <= MaxSlot; i++)
            {
                string slotKey = $"event-slot-{i}";
                var articleInSlot = await _dapr.GetStateAsync<V2Events>(storeName, slotKey, cancellationToken: cancellationToken);
                if (articleInSlot == null)
                {
                    emptySlot = i;
                    break;
                }
            }
            if (emptySlot == -1)
            {
                string lastSlotKey = $"event-slot-{MaxSlot}";
                var lastEvent = await _dapr.GetStateAsync<V2Events>(storeName, lastSlotKey, cancellationToken: cancellationToken);

                if (lastEvent != null)
                {
                    lastEvent.IsFeatured = false;
                    lastEvent.FeaturedSlot = new();
                    await _dapr.SaveStateAsync(storeName, lastEvent.Id.ToString(), lastEvent, cancellationToken: cancellationToken);
                    await _dapr.DeleteStateAsync(storeName, lastSlotKey, cancellationToken: cancellationToken);
                }

                emptySlot = MaxSlot;
            }
            for (int current = emptySlot - 1; current >= desiredSlot; current--)
            {
                string fromKey = $"event-slot-{current}";
                string toKey = $"event-slot-{current + 1}";

                var evToMove = await _dapr.GetStateAsync<V2Events>(storeName, fromKey, cancellationToken: cancellationToken);
                if (evToMove != null)
                {
                    evToMove.FeaturedSlot.Id = current + 1;
                    evToMove.IsFeatured = true;

                    await _dapr.SaveStateAsync(storeName, toKey, evToMove, cancellationToken: cancellationToken);
                    await _dapr.SaveStateAsync(storeName, evToMove.Id.ToString(), evToMove, cancellationToken: cancellationToken);
                    await _dapr.DeleteStateAsync(storeName, fromKey, cancellationToken : cancellationToken);
                }
            }

            newEvent.FeaturedSlot.Id = desiredSlot;
            newEvent.IsFeatured = true;

            await _dapr.SaveStateAsync(storeName, desiredSlotKey, newEvent, cancellationToken: cancellationToken);
            await _dapr.SaveStateAsync(storeName, newEvent.Id.ToString(), newEvent, cancellationToken: cancellationToken);

            return $"Event placed in slot {desiredSlot} after shifting.";
        }
        private void ValidateEventSchedule(EventSchedule schedule)
        {
            if (schedule == null)
                throw new ArgumentException("EventSchedule is required.");

            if (schedule.TimeSlotType == V2EventTimeType.PerDayTime)
            {
                if (schedule.TimeSlots == null || !schedule.TimeSlots.Any())
                    throw new ArgumentException("TimeSlots must be provided for 'PerDayTime' events.");

                if (schedule.StartTime != null || schedule.EndTime != null)
                    throw new ArgumentException("StartTime, and EndTime must be null for 'PerDayTime' events.");
            }
            else
            {
                if (schedule.StartDate > schedule.EndDate)
                    throw new ArgumentException($"StartDate ({schedule.StartDate:dd.MM.yyyy}) cannot be after EndDate ({schedule.EndDate:dd.MM.yyyy}).");

                if (schedule.StartDate == null || schedule.EndDate == null)
                    throw new ArgumentException("StartDate and EndDate must be provided for scheduled events.");

                if (schedule.StartTime == null || schedule.EndTime == null)
                    throw new ArgumentException("StartTime and EndTime must be provided for scheduled events.");

                if (schedule.TimeSlots != null && schedule.TimeSlots.Any())
                    throw new ArgumentException("TimeSlots must be empty for non-'PerDayTime' events.");
            }
        }
        private string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            var slug = title.Trim().ToLower()
                             .Replace(" ", "-")  
                             .Replace("--", "-");
            return slug;
        }
        public async Task<List<V2Events>> GetAllEvents(CancellationToken cancellationToken)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

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

                return events;
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while retrieving all events", ex);
            }
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
                ValidateEventSchedule(dto.EventSchedule);
                var existing = await _dapr.GetStateAsync<V2Events>(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    cancellationToken: cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException($"Event with ID {dto.Id} not found.");
                var slug = GenerateSlug(dto.EventTitle);
                var shouldUpdatePublishedDate = existing.Status == EventStatus.UnPublished && dto.Status == EventStatus.Published;
                var updated = new V2Events
                {
                    Id = dto.Id,
                    Slug = dto.Slug,
                    CategoryId = dto.CategoryId,
                    CategoryName = dto.CategoryName,
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
                    PublishedDate = shouldUpdatePublishedDate ? DateTime.UtcNow : existing.PublishedDate,
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

                return "Event updated successfully";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating events", ex);
            }
        }
        public async Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _dapr.GetStateAsync<V2Events>(
                ConstantValues.V2Content.ContentStoreName,
                id.ToString(),
                cancellationToken: cancellationToken);

            if (existing == null)
                throw new KeyNotFoundException($"Event with ID '{id}' not found.");

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(
                ConstantValues.V2Content.ContentStoreName,
                id.ToString(),
                existing,
                new StateOptions { Consistency = ConsistencyMode.Strong },
                cancellationToken: cancellationToken);

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
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventCategoryIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

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

                return categories;
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while retrieving all categories", ex);
            }
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

            return events;
        }

    }
}
