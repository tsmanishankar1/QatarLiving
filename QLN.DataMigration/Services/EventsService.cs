using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Utilities;
using System.Text;
using System.Text.Json;

namespace QLN.DataMigration.Services
{
    public class EventsService : IV2EventService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<EventsService> _logger;
        public EventsService(
            DaprClient dapr, 
            ILogger<EventsService> logger
            )
        {
            _dapr = dapr;
            _logger = logger;
        }

        public Task<string> CreateCategory(EventsCategory dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string> CreateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/event/createbyuserid";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                throw;
            }
        }

        public Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<EventsCategory>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2Events>> GetAllEvents(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2Slot>> GetAllEventSlot(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2Events>> GetAllIsFeaturedEvents(bool isFeatured, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<V2Events?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<EventsCategory?> GetEventCategoryById(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2Events>> GetEventsByStatus(EventStatus status, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2Events>> GetEventStatus(EventStatus status, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<V2Events>> GetExpiredEvents(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<V2Events>> GetPagedEvents(GetPagedEventsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> ReorderEventSlotsAsync(EventSlotReorderRequest dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> UnfeatureEvent(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> UpdateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateFeaturedEvent(UpdateFeaturedEvent dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
