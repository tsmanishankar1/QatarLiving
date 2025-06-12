using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Text.Json;

namespace QLN.Content.MS.Service
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
                var id = Guid.NewGuid();
                dto.Id = id;
                dto.CreatedBy = dto.UserId;
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedBy = null;
                dto.UpdatedAt = null;
                await _dapr.SaveStateAsync(ConstantValues.V2ContentEvents.ContentStoreName, dto.Id.ToString(), dto, cancellationToken: cancellationToken);

                var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.V2ContentEvents.ContentStoreName, ConstantValues.V2ContentEvents.EventIndexKey) ?? new List<string>();
                if (!keys.Contains(dto.Id.ToString()))
                {
                    keys.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(ConstantValues.V2ContentEvents.ContentStoreName, ConstantValues.V2ContentEvents.EventIndexKey, keys, cancellationToken: cancellationToken);
                }

                return "Event created successfully";
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating event", ex);
            }
        }
        public async Task<List<V2ContentEventDto>> GetAllEvents(CancellationToken cancellationToken)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.V2ContentEvents.ContentStoreName, ConstantValues.V2ContentEvents.EventIndexKey) ?? new();

                var items = await _dapr.GetBulkStateAsync(ConstantValues.V2ContentEvents.ContentStoreName, keys, null, cancellationToken: cancellationToken);

                var events = items
                    .Select(i => JsonSerializer.Deserialize<V2ContentEventDto>(i.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }))
                    .Where(e => e != null) 
                    .ToList();

                return events;
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
                var result = await _dapr.GetStateAsync<V2ContentEventDto>(ConstantValues.V2ContentEvents.ContentStoreName, id.ToString(), cancellationToken: cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving event with ID: {id}", ex);
            }
        }

        public async Task<string> UpdateEvent(V2ContentEventDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.Id == Guid.Empty)
                    throw new ArgumentException("Event ID is required for update.");

                var existing = await _dapr.GetStateAsync<V2ContentEventDto>(ConstantValues.V2ContentEvents.ContentStoreName, dto.Id.ToString(), null, cancellationToken:cancellationToken);
                dto.CreatedAt = existing.CreatedAt;
                dto.CreatedBy = existing.CreatedBy;
                dto.UpdatedBy = dto.UserId; 
                dto.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(ConstantValues.V2ContentEvents.ContentStoreName, dto.Id.ToString(), dto, cancellationToken: cancellationToken);

                return "Event updated successfully";
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating event", ex);
            }
        }

        public async Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _dapr.GetStateAsync<V2ContentEventDto>(ConstantValues.V2ContentEvents.ContentStoreName, id.ToString(), null, cancellationToken:cancellationToken);
            if (existing == null)
                throw new KeyNotFoundException($"Event with ID '{id}' not found.");

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(ConstantValues.V2ContentEvents.ContentStoreName, id.ToString(), existing, cancellationToken: cancellationToken);

            return "Event Soft Deleted Successfully";
        }
    }
}
