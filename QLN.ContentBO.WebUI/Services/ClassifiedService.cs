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
                _logger.LogError("GetFeaturedSeasonalPicks"+ex.Message);
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

        public async Task<HttpResponseMessage?> ReorderSeasonalPicksAsync(IEnumerable<object> slotAssignments,  string vertical)
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


    }
}
