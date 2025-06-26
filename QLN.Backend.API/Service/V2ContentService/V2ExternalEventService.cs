using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Utilities;
using System.Net;
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
        public async Task<string> CreateEvent(string userId, V2EventForm dto, CancellationToken cancellationToken = default)
        {
            string? FileName = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.CoverImage))
                {
                    var imageName = $"{dto.EventTitle}_{userId}.png";
                    var blobUrl = await _blobStorage.SaveBase64File(dto.CoverImage, imageName, "imageurl", cancellationToken);
                    dto.CoverImage = blobUrl;
                }
                var url = "/api/v2/event/createByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
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
                await CleanupUploadedFiles(FileName, cancellationToken);
                _logger.LogError(ex, "Error creating event");
                throw;
            }
        }
        private async Task CleanupUploadedFiles(string? file, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(file))
                await _blobStorage.DeleteFile(file, "CoverImage", cancellationToken);
        }
        public async Task<List<V2EventResponse>> GetAllEvents(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<List<V2EventResponse>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    "/api/v2/event/getAll",
                    cancellationToken
                ) ?? new List<V2EventResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all events.");
                throw;
            }
        }
        public async Task<V2EventResponse?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/event/getById/{id}";

                return await _dapr.InvokeMethodAsync<V2EventResponse>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event for Id : {Id}", id);
                throw;
            }
        }

        public async Task<string> UpdateEvent(string userId, V2UpdateRequest dto, CancellationToken cancellationToken = default)
        {
            string? FileName = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.CoverImage))
                {
                    var imageName = $"{dto.EventTitle}_{userId}.png";
                    var blobUrl = await _blobStorage.SaveBase64File(dto.CoverImage, imageName, "imageurl", cancellationToken);
                    dto.CoverImage = blobUrl;
                }
                var url = "/api/v2/event/updateByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Event with ID not found.");
                return null;
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(FileName, cancellationToken);
                _logger.LogError(ex, "Error updating event");
                throw;
            }
        }
        public async Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/event/delete/{id}";

                return await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    ConstantValues.V2Content.ContentServiceAppId,
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
