using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Text.Json;
using EventCategory = QLN.Common.DTO_s.EventCategory;

namespace QLN.Content.MS.Service.EventInternalService
{
    public class V2InternalEventService : IV2EventService
    {
        private readonly DaprClient _dapr;
        public V2InternalEventService(DaprClient dapr)
        {
            _dapr = dapr;
        }
        public async Task<string> CreateEvent(V2ContentEventDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var items = dto.QlnEvents.FeaturedEvents.Items;

                foreach (var eventItem in items)
                {
                    if (!EventCategory.Categories.ContainsKey(eventItem.Category))
                        throw new ArgumentException($"Invalid category: '{eventItem.Category}'.");

                    var id = Guid.NewGuid();
                    eventItem.Id = id;
                    eventItem.CreatedBy = eventItem.User_id;
                    eventItem.CreatedAt = DateTime.UtcNow;
                    eventItem.UpdatedBy = null;
                    eventItem.UpdatedAt = null;

                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        id.ToString(),
                        eventItem,
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
                }

                return "Event(s) created successfully";
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating event(s)", ex);
            }
        }
        public async Task<V2ContentEventDto> GetAllEvents(CancellationToken cancellationToken)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey) ?? new();

                var items = await _dapr.GetBulkStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    keys,
                    null,
                    cancellationToken: cancellationToken);

                var events = items
                    .Select(i => JsonSerializer.Deserialize<ContentEventDto>(i.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }))
                    .Where(e => e != null)
                    .ToList();

                return new V2ContentEventDto
                {
                    QlnEvents = new QlnEventsDto
                    {
                        FeaturedEvents = new QlnEventsQueueDto
                        {
                            QueueLabel = "Featured Events",
                            Items = events
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while retrieving all events", ex);
            }
        }
        public async Task<V2ContentEventDto?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.GetStateAsync<ContentEvent>(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    cancellationToken: cancellationToken);

                if (result == null)
                    return null;

                return new V2ContentEventDto
                {
                    QlnEvents = new QlnEventsDto
                    {
                        FeaturedEvents = new QlnEventsQueueDto
                        {
                            QueueLabel = "Featured Events",
                            Items = new List<ContentEventDto> { GetByIdDto(result) }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving event with ID: {id}", ex);
            }
        }

        private ContentEventDto GetByIdDto(ContentEvent entity)
        {
            return new ContentEventDto
            {
                Id = entity.Id,
                User_id = entity.User_id,
                IsActive = entity.IsActive,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt,
                UpdatedBy = entity.UpdatedBy,
                UpdatedAt = entity.UpdatedAt,
                CategroryId = entity.CategroryId,
                EntityOrganizer = entity.EntityOrganizer,
                EventCategory = entity.EventCategory,
                EventVenue = entity.EventVenue,
                EventStart = entity.EventStart,
                EventEnd = entity.EventEnd,
                EventLat = entity.EventLat,
                EventLong = entity.EventLong,
                EventLocation = entity.EventLocation,
                ImageUrl = entity.ImageUrl,
                Slug = entity.Slug,
                Nid = entity.Nid,
                DateCreated = entity.DateCreated,
                UserName = entity.UserName,
                ForumId = entity.ForumId,
                Title = entity.Title,
                Description = entity.Description,
                Category = entity.Category,
                Comments = entity.Comments
            };
        }
        public async Task<string> UpdateEvent(V2ContentEventDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var items = dto.QlnEvents?.FeaturedEvents?.Items;

                foreach (var eventItem in items)
                {
                    if (eventItem.Id == Guid.Empty)
                        throw new ArgumentException("Event ID is required for update.");

                    if (!EventCategory.Categories.ContainsKey(eventItem.Category))
                        throw new ArgumentException($"Invalid category: '{eventItem.Category}'.");

                    var existing = await _dapr.GetStateAsync<ContentEvent>(
                        ConstantValues.V2Content.ContentStoreName,
                        eventItem.Id.ToString(),
                        cancellationToken: cancellationToken);

                    if (existing == null)
                        throw new KeyNotFoundException($"Event with ID {eventItem.Id} not found.");

                    var updated = new ContentEvent
                    {
                        Id = eventItem.Id,
                        Title = eventItem.Title,
                        Description = eventItem.Description,
                        Category = eventItem.Category,
                        User_id = eventItem.User_id,
                        IsActive = eventItem.IsActive,
                        CategroryId = eventItem.CategroryId,
                        EntityOrganizer = eventItem.EntityOrganizer,
                        EventCategory = eventItem.EventCategory,
                        EventVenue = eventItem.EventVenue,
                        EventStart = eventItem.EventStart,
                        EventEnd = eventItem.EventEnd,
                        EventLat = eventItem.EventLat,
                        EventLong = eventItem.EventLong,
                        EventLocation = eventItem.EventLocation,
                        ImageUrl = eventItem.ImageUrl,
                        Slug = eventItem.Slug,
                        Nid = eventItem.Nid,
                        DateCreated = eventItem.DateCreated,
                        ForumId = eventItem.ForumId,
                        UserName = eventItem.UserName,
                        Comments = eventItem.Comments ?? existing.Comments,
                        CreatedAt = existing.CreatedAt,
                        CreatedBy = existing.CreatedBy,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = eventItem.User_id
                    };

                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        eventItem.Id.ToString(),
                        updated,
                        cancellationToken: cancellationToken);
                }

                return "All events updated successfully";
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating events", ex);
            }
        }

        public async Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _dapr.GetStateAsync<ContentEvent>(
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
    }
}
