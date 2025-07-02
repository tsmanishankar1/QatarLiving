using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Text.Json;

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
                    EventTitle = dto.EventTitle,
                    EventType = dto.EventType,
                    Price = dto.Price,
                    EventSchedule = dto.EventSchedule,
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
                if (dto.IsFeatured && dto.FeaturedSlot.Id >= 1 && dto.FeaturedSlot.Id <= 6)
                {
                    await HandleEventSlotShift(dto.FeaturedSlot.Id, entity, cancellationToken);
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
                var result = await _dapr.GetStateAsync<V2Events>(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    cancellationToken: cancellationToken);
                if (result == null)
                    throw new KeyNotFoundException($"Event with id '{id}' was not found.");
                if (!result.IsActive)
                    return null;
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving event with ID: {id}", ex);
            }
        }
        public async Task<string> UpdateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default)
        {
            try
            {
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
                    EventTitle = dto.EventTitle,
                    EventType = dto.EventType,
                    Price = dto.Price,
                    EventSchedule = dto.EventSchedule,
                    Location = dto.Location,
                    Venue = dto.Venue,
                    Longitude = dto.Longitude,
                    Latitude = dto.Latitude,
                    RedirectionLink = dto.RedirectionLink,
                    EventDescription = dto.EventDescription,
                    CoverImage = dto.CoverImage,
                    IsFeatured = dto.IsFeatured,
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
            try
            {
                var result = await _dapr.GetStateAsync<EventsCategory>(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    cancellationToken: cancellationToken);
                if (result == null)
                    throw new KeyNotFoundException($"Event with id '{id}' was not found.");
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving event with ID: {id}", ex);
            }
        }
        public async Task<PagedResponse<V2Events>> GetPagedEventCategories(int? page, int? perPage, string? search, int? sortBy, string? sortOrder, CancellationToken cancellationToken)
        {
            try
            {
                var categories = await _dapr.GetStateAsync<List<V2Events>>(
                    ConstantValues.V2Content.ContentStoreName,
                    "allCategories",
                    cancellationToken: cancellationToken);

                if (categories == null)
                    categories = new List<V2Events>();

                page ??= 1;
                perPage ??= 10;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    categories = categories.Where(c => c.CategoryId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                categories = sortBy switch
                {
                    1 => string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                        ? categories.OrderByDescending(c => c.CategoryId).ToList()
                        : categories.OrderBy(c => c.CategoryId).ToList(),
                    _ => categories
                };

                var totalCount = categories.Count;
                var paginatedCategories = categories
                    .Skip((page.Value - 1) * perPage.Value)
                    .Take(perPage.Value)
                    .ToList();

                return new PagedResponse<V2Events>
                {
                    TotalCount = totalCount,
                    Page = page,
                    PerPage = perPage,
                    Items = paginatedCategories
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching paginated event categories", ex);
            }
        }
        public async Task<List<V2Slot>> GetAllEventSlot(CancellationToken cancellationToken = default)
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
    }
}
