namespace QLN.DataMigration.Services
{
    using Microsoft.Extensions.Logging;
    using QLN.Common.DTO_s;
    using QLN.Common.Infrastructure.Constants;
    using QLN.Common.Infrastructure.DTO_s;
    using QLN.DataMigration.Models;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;

    public class DrupalSourceService : IDrupalSourceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DataOutputService> _logger;

        public DrupalSourceService(
            HttpClient httpClient, 
            ILogger<DataOutputService> logger
            )
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<DrupalItems?> GetItemsAsync(
            string environment,
            int categoryId,
            string sortField,
            string sortOrder,
            string? keywords,
            int? page,
            int? pageSize, 
            CancellationToken cancellationToken
            )
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("env", environment),
                new KeyValuePair<string, string>("category_id", categoryId.ToString()),
                new KeyValuePair<string, string>("sort_field", sortField),
                new KeyValuePair<string, string>("sort_order", sortOrder)
            };

            if(!string.IsNullOrEmpty(keywords))
            {
                formData.Add(new KeyValuePair<string, string>("keywords", keywords));
            }
            
            if (page != null)
            {
                formData.Add(new KeyValuePair<string, string>("page_size", pageSize.ToString()));
            }

            if(pageSize != null)
            {
                formData.Add(new KeyValuePair<string, string>("page", page.ToString()));
            }

            var content = new FormUrlEncodedContent(formData);

            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync(Constants.ItemsEndpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to migrate items. Status: {response.StatusCode}");
                return null;
            }
            _logger.LogInformation($"Got Response from migration endpoint {Constants.ItemsEndpoint}");
            var json = await response.Content.ReadAsStringAsync();
            try
            {
                var items = JsonSerializer.Deserialize<DrupalItems>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                _logger.LogInformation("Completed Deserialization");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Deserialization error: {ex.Message}");
                return null;
            }
        }

        public async Task<DrupalItemsCategories?> GetCategoriesAsync(string environment, CancellationToken cancellationToken)
        {

            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("env", environment)
            };
            var content = new FormUrlEncodedContent(formData);

            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync(Constants.CategoriesEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to migrate categories. Status: {response.StatusCode}");
                return null;
            }

            _logger.LogInformation($"Got Response from migration endpoint {Constants.CategoriesEndpoint}");

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                var categories = JsonSerializer.Deserialize<DrupalItemsCategories>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Completed Deserialization");
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Deserialization error: {ex.Message}");
                return null;
            }
        }

        public async Task<CategoriesResponse?> GetCategoriesFromDrupalAsync(CancellationToken cancellationToken)
        {
            return await _httpClient.GetFromJsonAsync<CategoriesResponse>(DrupalContentConstants.CategoriesPath, cancellationToken);
        }
        public async Task<ContentEventsResponse?> GetEventsFromDrupalAsync(
            CancellationToken cancellationToken,
            int? page = null,
            int? page_size = null
            )
        {
            page ??= 1;
            page_size ??= 30;

            string requestUri = $"{DrupalContentConstants.EventsPath}?page={page}&page_size={page_size}";

            return await _httpClient.GetFromJsonAsync<ContentEventsResponse>(requestUri, cancellationToken);
        }

        public async Task<CommunitiesResponse?> GetCommunitiesFromDrupalAsync(
            CancellationToken cancellationToken,
            int? page = null,
            int? page_size = null
            )
        {
            page ??= 1;
            page_size ??= 30;

            string requestUri = $"{DrupalContentConstants.CommunityPath}?page={page}&page_size={page_size}";

            return await _httpClient.GetFromJsonAsync<CommunitiesResponse>(requestUri, cancellationToken);
        }

        public async Task<ArticleResponse?> GetNewsFromDrupalAsync(
            string sourceCategory,
            CancellationToken cancellationToken,
            int? page = null,
            int? page_size = null
            )
        {
            page ??= 1;
            page_size ??= 30;

            string requestUri = $"{DrupalContentConstants.NewsPath}?page={page}&page_size={page_size}&forum_id={sourceCategory}";


            return await _httpClient.GetFromJsonAsync<ArticleResponse>(requestUri, cancellationToken);
        }
    }
}
