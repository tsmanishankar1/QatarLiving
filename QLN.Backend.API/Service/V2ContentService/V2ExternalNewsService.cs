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

                var url = "/api/v2/news/createNewBy-Id";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
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
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/getAllnews";

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
                var url = $"/api/v2/news/getById/{id}";

                return await _dapr.InvokeMethodAsync<V2ContentNewsDto>(
                    HttpMethod.Get,
                     ConstantValues.V2Content.ContentServiceAppId,
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

                var url = "/api/v2/news/updateNewsByUserId";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
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
                    ConstantValues.V2Content.ContentServiceAppId,
                    $"/api/v2/news/delete/{id}",
                    cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting news with ID: {NewsId}", id);
                return false;
            }
        }

        public async Task<string> CreateNewsCategoryAsync(NewsCategoryDto dto, CancellationToken cancellationToken = default)
        {
            try
            {

                var url = "/api/v2/news/createNewsCategory";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
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

        public async Task<List<NewsCategoryDto>> GetAllNewsCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/getAllnewsCategory";


                return await _dapr.InvokeMethodAsync<List<NewsCategoryDto>>(
                    HttpMethod.Get,
                    appId,
                    path,
                    cancellationToken
                ) ?? new List<NewsCategoryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all news");
                throw;
            }
        }
        public async Task<Dictionary<string, string>> GetWriterTagsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/getWriterTags";

                return await _dapr.InvokeMethodAsync<Dictionary<string, string>>(
               HttpMethod.Get,
               appId,
               path,
               cancellationToken
           ) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving writer tags from internal service");
                throw;
            }
        }
        public async Task<List<V2NewsCategory>> GetNewsCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/getcategories";

                return await _dapr.InvokeMethodAsync<List<V2NewsCategory>>(
               HttpMethod.Get,
               appId,
               path,
               cancellationToken
           ) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving writer tags from internal service");
                throw;
            }
        }
        public async Task<List<V2Slot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/slots";

                return await _dapr.InvokeMethodAsync<List<V2Slot>>(
               HttpMethod.Get,
               appId,
               path,
               cancellationToken
           ) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving writer tags from internal service");
                throw;
            }
        }
        public async Task<CreateNewsArticleResponseDto> CreateNewsArticleAsync(Guid userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Set unique ID if not already set
                dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
                dto.CreatedBy = userId;
                dto.UpdatedBy = userId;
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;

                // Upload cover image
                if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl))
                {
                    var imageName = $"{dto.Title}_{dto.Id}.png";
                    var blobUrl = await _blobStorage.SaveBase64File(dto.CoverImageUrl, imageName, "imageurl", cancellationToken);
                    dto.CoverImageUrl = blobUrl;
                }

                var url = "/api/v2/news/createNewsArticleById";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<CreateNewsArticleResponseDto>(rawJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Empty or invalid response from content service.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news article via external service");
                throw;
            }
        }
        public async Task<List<V2NewsArticleDTO>> GetAllNewsArticlesAsync(CancellationToken cancellationToken = default)
        {
            var url = "/api/v2/news/getAllNewsArticle";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.V2Content.ContentServiceAppId, url);

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var rawJson = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<V2NewsArticleDTO>>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("Failed to retrieve articles.");
        }

        public async Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
        {
            var url = $"api/v2/news/byCategory/{categoryId}";
            var response = await _dapr.InvokeMethodAsync<List<V2NewsArticleDTO>>(
                HttpMethod.Get,
                V2Content.ContentServiceAppId,
                url,
                cancellationToken
            );
            return response;
        }

        public async Task<List<V2NewsArticleDTO>> GetArticlesBySubCategoryIdAsync(int categoryId, int subCategoryId, CancellationToken cancellationToken)
        {
            var url = $"api/v2/news/byCategory/{categoryId}/sub/{subCategoryId}";
            var response = await _dapr.InvokeMethodAsync<List<V2NewsArticleDTO>>(
                HttpMethod.Get,
                V2Content.ContentServiceAppId,
                url,
                cancellationToken
            );
            return response;
        }
        public async Task<string> UpdateNewsArticleAsync(V2NewsArticleDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl))
                {
                    var imageName = $"{dto.Title}_{dto.Id}.png";
                    var blobUrl = await _blobStorage.SaveBase64File(dto.CoverImageUrl, imageName, "imageurl", cancellationToken);
                    dto.CoverImageUrl = blobUrl;
                }
                var url = "/api/v2/news/updateNewsarticleByUserId"; 
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update article via external service");
                throw;
            }
        }
    }
}
