namespace QLN.DataMigration.Services
{
    using Microsoft.Extensions.Logging;
    using QLN.Common.Infrastructure.Constants;
    using QLN.DataMigration.Models;
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
            int? pageSize)
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

        public async Task<DrupalItemsCategories?> GetCategoriesAsync(string environment)
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
    }
}
