using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Text.Json;

namespace QLN.Content.MS.Service
{
    public class InternalEventService : IEventService
    {
        private readonly DaprClient _dapr;
        private const string StoreName = "statestore";
        private const string IndexKey = "event-index";

        public InternalEventService(DaprClient dapr)
        {
            _dapr = dapr;
        }

        public async Task<string> CreateEvent(ContentEventDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                dto.id = id;
                dto.CreatedAt = DateTime.UtcNow;

                var key = id.ToString();
                await _dapr.SaveStateAsync(StoreName, key, dto, cancellationToken: cancellationToken);

                var keys = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey) ?? new List<string>();
                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    await _dapr.SaveStateAsync(StoreName, IndexKey, keys, cancellationToken: cancellationToken);
                }

                return "Event created successfully";
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating event", ex);
            }
        }
        public async Task<List<ContentEventDto>> GetAllEvents(CancellationToken cancellationToken)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey) ?? new();

                var items = await _dapr.GetBulkStateAsync(StoreName, keys, null, cancellationToken: cancellationToken);

                var events = items
                    .Select(i => JsonSerializer.Deserialize<ContentEventDto>(i.Value, new JsonSerializerOptions
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

        public async Task<ContentEventDto?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            var key = id.ToString();

            try
            {
                var result = await _dapr.GetStateAsync<ContentEventDto>(StoreName, key, cancellationToken: cancellationToken);

                if (result == null)
                    throw new KeyNotFoundException($"Event with ID '{id}' was not found.");

                if (!result.IsActive)
                    throw new KeyNotFoundException($"Event with ID '{id}' is inactive (soft deleted).");

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving event with ID: {id}", ex);
            }
        }

        public async Task<string> UpdateEvent(ContentEventDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.id == Guid.Empty)
                    throw new ArgumentException("Event ID is required for update.");

                var existing = await _dapr.GetStateAsync<ContentEventDto>(StoreName, dto.id.ToString(), null, cancellationToken:cancellationToken);
                if (existing == null)
                    throw new KeyNotFoundException($"Event with ID '{dto.id}' was not found.");

                dto.CreatedAt = existing.CreatedAt;
                dto.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, dto.id.ToString(), dto, cancellationToken: cancellationToken);

                return "Event updated successfully";
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating event", ex);
            }
        }

        public async Task<bool> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            var key = id.ToString();

            var existing = await _dapr.GetStateAsync<ContentEventDto>(StoreName, key, null, cancellationToken:cancellationToken);
            if (existing == null)
                throw new KeyNotFoundException($"Event with ID '{id}' not found.");

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(StoreName, key, existing, cancellationToken: cancellationToken);

            return true;
        }
    }
}
