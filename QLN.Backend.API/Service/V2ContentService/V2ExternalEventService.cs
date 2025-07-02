using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
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
        public async Task<string> CreateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default)
        {
            string? FileName = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.CoverImage))
                {
                    var (ext, base64Data) = Base64Helper.ParseBase64(dto.CoverImage);
                    if (ext is not ("jpeg" or "png" or "jpg"))
                        throw new ArgumentException("Cover Image must be in Jpeg, PNG, or JPG format.");
                    var imageName = $"{dto.EventTitle}_{userId}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, imageName, "imageurl", cancellationToken);
                    dto.CoverImage = blobUrl;
                }
                var url = "/api/v2/event/createByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.BadRequest)
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
                    await CleanupUploadedFiles(FileName, cancellationToken);
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

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
        public async Task<List<V2Events>> GetAllEvents(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<List<V2Events>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    "/api/v2/event/getAll",
                    cancellationToken
                ) ?? new List<V2Events>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all events.");
                throw;
            }
        }
        public async Task<V2Events?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/event/getById/{id}";

                return await _dapr.InvokeMethodAsync<V2Events>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event for Id : {Id}", id);
                throw;
            }
        }
        public async Task<string> UpdateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default)
        {
            string? FileName = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.CoverImage) && !dto.CoverImage.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var (ext, base64Data) = Base64Helper.ParseBase64(dto.CoverImage);
                    if (ext is not ("jpeg" or "png" or "jpg"))
                        throw new ArgumentException("Cover Image must be in Jpeg, PNG, or JPG format.");
                    var imageName = $"{dto.EventTitle}_{userId}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, imageName, "imageurl", cancellationToken);
                    dto.CoverImage = blobUrl;
                }

                var url = "/api/v2/event/updateByUserId";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.BadRequest)
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
                    await CleanupUploadedFiles(FileName, cancellationToken);
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
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
        public async Task<string> CreateCategory(EventsCategory dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/event/createCategory";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.BadRequest)
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
                _logger.LogError(ex, "Error creating event category");
                throw;
            }
        }
        public async Task<List<EventsCategory>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<List<EventsCategory>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    "/api/v2/event/getAllCategories",
                    cancellationToken
                ) ?? new List<EventsCategory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all event categories.");
                throw;
            }
        }
        public async Task<EventsCategory?> GetEventCategoryById(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/event/getCategoryById/{id}";

                return await _dapr.InvokeMethodAsync<EventsCategory>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event for Id : {Id}", id);
                throw;
            }
        }
        public async Task<PagedResponse<V2Events>> GetPagedEventCategories(int? page, int? perPage, string? search, int? sortBy, string? sortOrder, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"/api/v2/event/getPagination?page={page}&perPage={perPage}&search={search}&sortBy={sortBy}&sortOrder={sortOrder}";
                return await _dapr.InvokeMethodAsync<PagedResponse<V2Events>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged event categories.");
                throw;
            }
        }
        public async Task<string> StatusChange(string uid, Guid id, EventStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/event/statusChangeByUserId?id={id}&eventStatus={status}&updatedBy={uid}";
                return await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Put,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing status for event with Id : {Id}", id);
                throw;
            }
        }
        public async Task<IEnumerable<V2FeaturedEvents>> GetEventSummaries(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/event/getEventStatus";
                return await _dapr.InvokeMethodAsync<IEnumerable<V2FeaturedEvents>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken
                ) ?? new List<V2FeaturedEvents>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event summaries.");
                throw;
            }
        }
    }
}
