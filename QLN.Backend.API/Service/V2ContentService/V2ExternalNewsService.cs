using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using System.Text;
using System.Text.Json;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalNewsService : IV2NewsService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalNewsService> _logger;
        private readonly IFileStorageBlobService _blobStorage;

        public V2ExternalNewsService(
            DaprClient dapr,
            ILogger<V2ExternalNewsService> logger,
            IFileStorageBlobService blobStorage)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
        }

        public async Task<string> CreateNews(V2ContentNewsDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.Image_url))
                {
                    var imageName = $"{dto.title}_{dto.UserId}.png";
                    var blobUrl = await _blobStorage.SaveBase64File(dto.Image_url, imageName, "imageurl", cancellationToken);
                    dto.Image_url = blobUrl;
                }

                var url = "/v2/api/News/createNewBy-Id";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2ContentNews.NewsServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news");
                throw;
            }
        }


        public async Task<List<V2ContentNewsDto>> GetAllNews(CancellationToken cancellationToken = default)
        {
            try
            {
                var appId = ConstantValues.V2ContentNews.NewsServiceAppId;
                var path = "/v2/api/News/getAllnews";

                _logger.LogInformation("Calling Dapr method: AppId = {AppId}, Path = {Path}", appId, path);

                return await _dapr.InvokeMethodAsync<List<V2ContentNewsDto>>(
                    HttpMethod.Get,
                    appId,
                    path,
                    cancellationToken
                ) ?? new List<V2ContentNewsDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all news");
                throw;
            }
        }


        public async Task<V2ContentNewsDto?> GetNewsById(Guid id, CancellationToken cancellationToken = default)
        {
            //try
                try
            {
                var url = $"/v2/api/News/getById/{id}";

                return await _dapr.InvokeMethodAsync<V2ContentNewsDto>(
                    HttpMethod.Get,
                     ConstantValues.V2ContentNews.NewsServiceAppId,
                    url,
                    cancellationToken);
            }
            //{
            //    var url = $"/v2/api/News/getById/{id}";
            //    var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.V2ContentNews.NewsServiceAppId, url);

            //    var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

            //    if (!response.IsSuccessStatusCode)
            //    {
            //        _logger.LogWarning("News with ID {Id} not found. Status: {Status}", id, response.StatusCode);
            //        return null;
            //    }

            //    var json = await response.Content.ReadAsStringAsync();
            //    return JsonSerializer.Deserialize<V2ContentNewsDto>(json);
            //}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching news by ID");
                throw;
            }
        }


        public async Task<string> UpdateNews(V2ContentNewsDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.Image_url))
                {
                    var imageName = $"{dto.title}_{dto.UserId}.png";
                    var blobUrl = await _blobStorage.SaveBase64File(dto.Image_url, imageName, "imageurl", cancellationToken);
                    dto.Image_url = blobUrl;
                }

                var url = "/v2/api/News/updateNewsByUserId";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2ContentNews.NewsServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating news with ID: {NewsId}", dto.Id);
                throw;
            }
        }

        public async Task<bool> DeleteNews(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete,
                    ConstantValues.V2ContentNews.NewsServiceAppId,
                    $"/v2/api/News/delete/{id}",
                    cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting news with ID: {NewsId}", id);
                return false;
            }
        }
    }
}
