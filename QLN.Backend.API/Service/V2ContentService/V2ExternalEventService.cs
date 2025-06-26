using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalEventService : IV2EventService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalEventService> _logger;
        private readonly IFileStorageBlobService _blobStorage;
        public V2ExternalEventService(DaprClient dapr, ILogger<V2ExternalEventService> logger, IFileStorageBlobService blobStorage)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
        }

        public async Task<string> CreateEvent(V2ContentEventDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.Image_url))
                {
                    var imageName = $"{dto.Title} _ {dto.User_id}.png";
                    var BlobUrl = await _blobStorage.SaveBase64File(dto.Image_url, imageName, "imageurl", cancellationToken);
                    dto.Image_url = BlobUrl;
                }
                var url = "/v2/api/event/createByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2ContentEvents.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
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

        public async Task<List<V2ContentEventDto>> GetAllEvents(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<List<V2ContentEventDto>>(
                    HttpMethod.Get,
                    ConstantValues.V2ContentEvents.ContentServiceAppId,
                    "/v2/api/event/getAll",
                    cancellationToken
                ) ?? new List<V2ContentEventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all events.");
                throw;
            }
        }

        public async Task<V2ContentEventDto?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/v2/api/event/getById/{id}";

                return await _dapr.InvokeMethodAsync<V2ContentEventDto>(
                    HttpMethod.Get,
                    ConstantValues.V2ContentEvents.ContentServiceAppId,
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

        public async Task<string> UpdateEvent(V2ContentEventDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.Image_url))
                {
                    var imageName = $"{dto.Title}_{dto.User_id}.png";
                    var BlobUrl = await _blobStorage.SaveBase64File(dto.Image_url, imageName, "imageurl", cancellationToken);
                    dto.Image_url = BlobUrl;
                }
                var url = "/v2/api/event/updateByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2ContentEvents.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Event with ID not found.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event");
                throw;
            }
        }

        public async Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/v2/api/event/delete/{id}";

                return await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    ConstantValues.V2ContentEvents.ContentServiceAppId,
                    url,
                    cancellationToken
                );
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Event with ID {id} not found.", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event with Id {id}", id);
                throw;
            }
        }
    }
}
