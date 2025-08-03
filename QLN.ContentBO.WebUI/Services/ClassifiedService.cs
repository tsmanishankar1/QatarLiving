using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static MudBlazor.CategoryTypes;

namespace QLN.ContentBO.WebUI.Services
{
    public class ClassifiedService : ServiceBase<ClassifiedService>, IClassifiedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        public ClassifiedService(HttpClient httpClient, ILogger<ClassifiedService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }
        public async Task<HttpResponseMessage?> GetAllCategoryTreesAsync(string vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/classified/category/{vertical}/all-trees");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllCategoryTreesAsync Error: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetFeaturedSeasonalPicks(string vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/v2/classifiedbo/seasonal-picks/slotted?vertical={vertical}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFeaturedSeasonalPicks" + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAllSeasonalPicks(string vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/v2/classifiedbo/getSeasonalPicks?vertical={vertical}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFeaturedSeasonalPicks" + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> CreateSeasonalPicksAsync(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/seasonal-picks")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                Console.WriteLine("CreateSeasonalPicksAsync" + request);
                Console.WriteLine("CreateSeasonalPicksAsync Payload:\n" + json);

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateSeasonalPicksAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }


        public async Task<HttpResponseMessage?> ReplaceSeasonalPickAsync(string pickId, int slot, string vertical)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put,
                    $"/api/v2/classifiedbo/seasonal-picks/replace-slot?pickId={pickId}&slot={slot}&vertical={vertical}");

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("ReplaceSeasonalPickAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> DeleteSeasonalPicks(string pickId, string vertical)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"/api/v2/classifiedbo/seasonal-picks/soft-delete?pickId={pickId}&Vertical={vertical}");

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteSeasonalPicks: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> ReorderSeasonalPicksAsync(IEnumerable<object> slotAssignments, string vertical)
        {
            try
            {
                var payload = new
                {
                    slotAssignments = slotAssignments,
                    vertical = vertical
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Reorder Seasonal Picks Payload:\n" + json);

                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/classifiedbo/seasonal-picks/reorder-slots")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("ReorderSeasonalPicksAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetFeaturedCategory(string vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/v2/classifiedbo/getslottedfeaturedcategory?vertical={vertical}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFeaturedCategory" + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAllFeatureCategory(string vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/v2/classifiedbo/getfeaturedcategoriesbyvertical/{vertical}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllFeatureCategory" + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> CreateFeaturedCategoryAsync(object payload)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(payload, options);

                Console.WriteLine("Serialized Payload:");
                Console.WriteLine(json);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/createfeaturedcategory")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateFeaturedCategoryAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }


        public async Task<HttpResponseMessage?> ReplaceFeaturedCategoryAsync(string pickId, int slot, string vertical)
        {
            try
            {
                var body = new
                {
                    categoryId = pickId,
                    targetSlotId = slot,
                    vertical = vertical
                };

                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                Console.WriteLine("Reorder Seasonal Picks Payload:\n" + json);

                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/classifiedbo/replacefeaturedcategoryslots")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("ReplaceFeaturedCategoryAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> DeleteFeaturedCategory(string pickId, string vertical)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"/api/v2/classifiedbo/featured-category-delete?categoryId={pickId}&vertical={vertical}");

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteFeaturedCategory: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> ReorderFeaturedCategoryAsync(IEnumerable<object> slotAssignments, string vertical)
        {
            try
            {
                var payload = new
                {
                    slotAssignments = slotAssignments,
                    vertical = vertical
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Reorder Seasonal Picks Payload:\n" + json);

                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/classifiedbo/featured-category/reorder-slots")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("ReorderFeaturedCategoryAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        /// <summary>
        /// Searches classifieds by vertical using a common API pattern.
        /// </summary>
        /// <param name="vertical">The vertical segment of the API URL (e.g., "getall-items")</param>
        /// <param name="searchPayload">The request payload</param>
        /// <returns>A list of HttpResponseMessage</returns>
        public async Task<List<HttpResponseMessage>> SearchClassifiedsViewListingAsync(string vertical, object searchPayload)
        {
            var responses = new List<HttpResponseMessage>();

            try
            {
                var endpoint = $"/api/v2/classifiedbo/{vertical}";
                var response = await _httpClient.PostAsJsonAsync(endpoint, searchPayload);
                responses.Add(response);
                return responses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SearchClassifiedsAsync Error for {vertical}: " + ex);
                responses.Add(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                return responses;
            }
        }
        public async Task<List<HttpResponseMessage>> SearchClassifiedsViewTransactionAsync(object searchPayload)
        {
            var responses = new List<HttpResponseMessage>();

            try
            {
                var endpoint = $"/api/v2/classifiedbo/items/transactions";
                var response = await _httpClient.PostAsJsonAsync(endpoint, searchPayload);
                responses.Add(response);
                return responses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SearchClassifiedsViewTransactionAsync Error for " + ex);
                responses.Add(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                return responses;
            }
        }
        public async Task<HttpResponseMessage?> PerformBulkActionAsync(string vertical, object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v2/classifiedbo/{vertical}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("PerformBulkActionAsync Error: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetPrelovedListingsAsync(FilterRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/getall-preloved")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(httpRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPrelovedListingsAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAdByIdAsync(string vertical, string adId)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/classified/{vertical}/{adId}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAdByIdAsync Error: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetAllZonesAsync()
        {
            try
            {
                return await _httpClient.GetAsync("/api/v2/location/getAllZones");
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllZonesAsync Error: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAddressByDetailsAsync(int zone, int street, int building, string location)
        {
            try
            {
                var url = $"/api/v2/location/findAddress?zone={zone}&street={street}&building={building}&location={Uri.EscapeDataString(location)}";
                return await _httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAddressByDetailsAsync Error: {ex}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> PostAdAsync(string vertical, object payload)
        {
            try
            {
                var endpoint = $"/api/classified/{vertical}";

                // Create request manually with correct headers
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    })
                };

                // Send request
                var response = await _httpClient.SendAsync(request);

                Console.WriteLine($"Post response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error body: {errorBody}");
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("HTTP request timed out.");
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error in PostClassifiedItemAsync: {ex}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> UpdateAdAsync(string vertical, object payload)
        {
            try
            {
                var endpoint = $"/api/classified/{vertical}/update";

                using var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    })
                };

                var response = await _httpClient.SendAsync(request);

                Console.WriteLine($"Update response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error body: {errorBody}");
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("HTTP request timed out.");
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error in UpdateAdAsync: {ex}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> UplodAsync(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var request = new HttpRequestMessage(HttpMethod.Post, $"/files/upload")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                Console.WriteLine("PostAdAsync Request Payload:");
                Console.WriteLine(json);

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("PostAdAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> RefreshAdAsync(string adId, int subVertical)
        {
            try
            {
                var url = $"/api/classified/items/refresh/{adId}?subVertical={subVertical}";
                //  _logger.LogInformation("Calling RefreshAd API at URL: {Url}", url);
                var response = await _httpClient.PostAsync(url, null); // No body required
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to refresh ad {adId}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

    }
}
