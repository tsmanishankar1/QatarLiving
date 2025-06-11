using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.ContentService
{
    public class ExternalEventService : IEventService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalEventService> _logger;
        private const string AppId = "qln-event-ms";

        public ExternalEventService(DaprClient dapr, ILogger<ExternalEventService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateEvent(ContentEventDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/v2/api/event/create";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, AppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                throw;
            }
        }

        public async Task<List<ContentEventDto>> GetAllEvents(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<List<ContentEventDto>>(
                    HttpMethod.Get,
                    AppId,
                    "/v2/api/event/getAll",
                    cancellationToken
                ) ?? new List<ContentEventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all events.");
                throw;
            }
        }

        public async Task<ContentEventDto?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/v2/api/event/getById/{id}";

                return await _dapr.InvokeMethodAsync<ContentEventDto>(
                    HttpMethod.Get,
                    AppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Event with id '{id}' not found.", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event for Id : {Id}", id);
                throw;
            }
        }

        public async Task<string> UpdateEvent(ContentEventDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/v2/api/event/update";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, AppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event");
                throw;
            }
        }

        public async Task<bool> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/v2/api/event/delete/{id}";

                return await _dapr.InvokeMethodAsync<bool>(
                    HttpMethod.Delete,
                    AppId,
                    url,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event with Id {id}", id);
                throw;
            }
        }
    }
}
