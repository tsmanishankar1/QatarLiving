using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using System.Net;
using System.Text;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
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

        public async Task<WriterTagsResponse> GetWriterTagsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/writertags";

                return await _dapr.InvokeMethodAsync<WriterTagsResponse>(
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
     
        public async Task<List<V2NewsSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/slots";

                return await _dapr.InvokeMethodAsync<List<V2NewsSlot>>(
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
        public async Task<string> CreateNewsArticleAsync(string userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {
                dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
                dto.CreatedBy = userId;
                dto.UpdatedBy = userId;
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;

                // Upload image to blob storage if present
                if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl))
                {
                    var imageName = $"{dto.Title}_{dto.Id}.png";
                    dto.CoverImageUrl = await _blobStorage.SaveBase64File(dto.CoverImageUrl, imageName, "imageurl", cancellationToken);
                }

                var url = "/api/v2/news/createNewsArticleById";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? throw new Exception("Empty or invalid response from content service.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news article via external service");
                throw;
            }
        }

        public async Task<PagedResponse<V2NewsArticleDTO>> GetAllNewsArticlesAsync(
        int? page, int? perPage, string? search, CancellationToken cancellationToken = default)
        {
            try
            {
                var queryParams = new List<string>
    {
        $"page={page ?? 1}",
        $"perPage={perPage ?? 12}",
        $"search={Uri.EscapeDataString(search ?? "")}"
    };

                var url = $"/api/v2/news/news?{string.Join("&", queryParams)}";
                return await _dapr.InvokeMethodAsync<PagedResponse<V2NewsArticleDTO>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
        {
            var url = $"api/v2/news/categories/{categoryId}";
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
            var url = $"api/v2/news/categories/{categoryId}/sub/{subCategoryId}";
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
                if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl) && !dto.CoverImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
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

        public async Task<List<V2NewsArticleDTO>> GetAllNewsFilterArticles(bool? isActive, CancellationToken cancellationToken = default)
        {
            try
            {
                // Build URL with query string
                string queryParam = isActive.HasValue ? $"?isActive={isActive.Value.ToString().ToLower()}" : string.Empty;
                var url = $"/api/v2/news/filterbystatus{queryParam}";

                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId, // Your News service's Dapr App ID
                    url);

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<V2NewsArticleDTO>>(rawJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Failed to deserialize filtered articles.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetNewsArticlesByIsActiveAsync");
                throw;
            }
        }

        public async Task<string> DeleteNews(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/news/news/{id}";

                return await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken
                );
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "News with ID {id} not found.", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting News with Id {id}", id);
                throw;
            }
        }
        public async Task<string> ReorderSlotsAsync(ReorderSlotRequestDto dto, CancellationToken cancellationToken)

        {
            try
            {
                var url = "/api/v2/news/reorderLiveSlotsByUserId";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply slot rearrangement via external service");
                throw;
            }
        }

        public async Task<V2NewsArticleDTO?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var url = $"/api/v2/news/getbyid/{id}";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.V2Content.ContentServiceAppId, url);

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var rawJson = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<V2NewsArticleDTO>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<V2NewsArticleDTO?> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var url = $"/api/v2/news/getbyslug/{slug}";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.V2Content.ContentServiceAppId, url);

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Return null if not found (optional: log or track)
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Failed to fetch article. Status: {response.StatusCode}, Body: {errorContent}");
            }

            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonSerializer.Deserialize<V2NewsArticleDTO>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }


        public async Task AddCategoryAsync(V2NewsCategory category, CancellationToken cancellationToken = default)
        {
            var url = "/api/v2/news/category/createById";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
            request.Content = new StringContent(JsonSerializer.Serialize(category), Encoding.UTF8, "application/json");

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<V2NewsCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var url = "/api/v2/news/allcategories";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.V2Content.ContentServiceAppId, url);

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<V2NewsCategory>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }

        public async Task<V2NewsCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var url = $"/api/v2/news/categorygetbyid/{id}";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.V2Content.ContentServiceAppId, url);

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<V2NewsCategory>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> UpdateSubCategoryAsync(int categoryId, V2NewsSubCategory updatedSubCategory, CancellationToken cancellationToken = default)
        {
            var url = $"/api/v2/news/category/subcategorybyid?categoryId={categoryId}";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
            request.Content = new StringContent(JsonSerializer.Serialize(updatedSubCategory), Encoding.UTF8, "application/json");

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return false;

            response.EnsureSuccessStatusCode();
            return true;
        }



    }
}
