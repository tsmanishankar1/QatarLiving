using Dapr.Client;
using Microsoft.AspNetCore.Http;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Security.Claims;
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
        public async Task<string> CreateEvent(string userId, V2EventForm dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.EventType == V2EventType.FeePrice)
                {
                    if (dto.Price == null)
                        throw new InvalidDataException("Price must be provided when the EventType is 'Fees'.");
                }
                else
                {
                    if (dto.Price != null)
                        throw new InvalidDataException("Price must not be entered for 'Free Access' or 'Open Registration' events.");
                }
                var id = Guid.NewGuid();

                var entity = new V2EventResponse
                {
                    Id = id,
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
            catch (Exception ex)
            {
                throw new Exception("Error creating event", ex);
            }
        }
        public async Task<List<V2EventResponse>> GetAllEvents(CancellationToken cancellationToken)
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
                    .Select(i => JsonSerializer.Deserialize<V2EventResponse>(i.Value, new JsonSerializerOptions
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
        public async Task<V2EventResponse?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.GetStateAsync<V2EventResponse>(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    cancellationToken: cancellationToken);
                if (result == null || result.IsActive == false)
                    throw new KeyNotFoundException($"Active event with ID '{id}' not found.");
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving event with ID: {id}", ex);
            }
        }
        public async Task<string> UpdateEvent(string userId, V2UpdateRequest dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.Id == Guid.Empty)
                    throw new InvalidDataException("Event ID is required for update.");
                if (dto.EventType == V2EventType.FeePrice)
                {
                    if (dto.Price == null)
                        throw new InvalidDataException("Price must be provided when the EventType is 'Fees'.");
                }
                else
                {
                    if (dto.Price != null)
                        throw new InvalidDataException("Price must not be entered for 'Free Access' or 'Open Registration' events.");
                }
                var existing = await _dapr.GetStateAsync<V2EventResponse>(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    cancellationToken: cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException($"Event with ID {dto.Id} not found.");

                var updated = new V2EventResponse
                {
                    Id = dto.Id,
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
            catch (Exception ex)
            {
                throw new Exception("Error updating events", ex);
            }
        }

        public async Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _dapr.GetStateAsync<V2EventResponse>(
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
        public async Task<List<EventsCategory>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(new List<EventsCategory>
           {
               new EventsCategory { Id = 1, CategoryName = "Awareness" },
               new EventsCategory { Id = 2, CategoryName = "Classes & Workshops" },
               new EventsCategory { Id = 3, CategoryName = "Conferences" },
               new EventsCategory { Id = 4, CategoryName = "Entertainment" },
               new EventsCategory { Id = 5, CategoryName = "Exhibition" },
               new EventsCategory { Id = 6, CategoryName = "Festivals" },
               new EventsCategory { Id = 7, CategoryName = "Fundraisers" },
               new EventsCategory { Id = 8, CategoryName = "Lifestyle" },
               new EventsCategory { Id = 9, CategoryName = "Meetings & Networking" },
               new EventsCategory { Id = 10, CategoryName = "Music" },
               new EventsCategory { Id = 11, CategoryName = "Other" },
               new EventsCategory { Id = 12, CategoryName = "Performing Arts" },
               new EventsCategory { Id = 13, CategoryName = "Social Events" },
               new EventsCategory { Id = 14, CategoryName = "Sports" },
               new EventsCategory { Id = 15, CategoryName = "Training" }
           });
        }
    }
}
