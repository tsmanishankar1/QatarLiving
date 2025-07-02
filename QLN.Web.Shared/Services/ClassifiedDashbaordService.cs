using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Pages.Classifieds.Dashboards;
using QLN.Web.Shared.Services.Interface;
using System.Text;
using System.Text.Json;
using static QLN.Web.Shared.Models.ClassifiedsDashboardModel;

namespace QLN.Web.Shared.Services
{
    public class ClassfiedDashboardService : ServiceBase<ClassfiedDashboardService>, IClassifiedDashboardService
    {
        private readonly HttpClient _httpClient;

        private readonly ILogger<ClassfiedDashboardService> _logger;

        public ClassfiedDashboardService(HttpClient httpClient , ILogger<ClassfiedDashboardService> logger) : base(httpClient)
        {
            _httpClient = httpClient;
             _logger = logger;

        }
        /// <summary>
        /// Gets Classified Items Dashboard data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        public async Task<ItemDashboardResponse?> GetItemDashboard()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/classified/itemsAd-dashboard");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ItemDashboardResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetItemDashboard Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<List<AdModal>?> GetPublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/items/user-ads/published?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPublishedAds Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<List<AdModal>?> GetUnpublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/items/user-ads/unpublished?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUnpublishedAds Exception: " + ex.Message);
                return null;
            }
        }


        public async Task<PreLovedDashboardResponse?> GetPreLovedDashboard()
        {

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/classified/prelovedAd-dashboard");
                

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<PreLovedDashboardResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetItemDashboard Exception: " + ex.Message);
                return null;
            }
        }
        public async Task<List<AdModal>?> GetPreLovedPublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/preloved/user-ads/published?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPublishedAds Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<List<AdModal>?> GetPreLovedUnPublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/preloved/user-ads/unpublished?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUnpublishedAds Exception: " + ex.Message);
                return null;
            }
        }
        public async Task<List<AdModal>?> GetCollectiblesPublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/collectibles/user-ads/published?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPublishedAds Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<List<AdModal>?> GetCollectiblesUnPublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/collectibles/user-ads/unpublished?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUnpublishedAds Exception: " + ex.Message);
                return null;
            }
        }
        public async Task<List<AdModal>?> GetStoresPublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/stores/user-ads/published?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPublishedAds Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<List<AdModal>?> GetStoresUnPublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/stores/user-ads/unpublished?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUnpublishedAds Exception: " + ex.Message);
                return null;
            }
        }
        public async Task<List<AdModal>?> GetDealsPublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/deals/user-ads/published?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPublishedAds Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<List<AdModal>?> GetDealsUnPublishedAds(int page, int pageSize, string search, int sortOption)
        {
            try
            {
                var url = $"api/classified/deals/user-ads/unpublished?page={page}&pageSize={pageSize}&sortOption={sortOption}&search={search}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AdListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Items ?? new List<AdModal>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUnpublishedAds Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<bool> PublishAdAsync(string adId)
        {
            try
            {
                var url = "api/classified/items/user-ads/publish";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var payload = new[] { adId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("PublishAdAsync Exception: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> PublishBulkAdsAsync(List<string> adIds)
        {
            try
            {
                var url = "api/classified/items/user-ads/unpublish";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var json = JsonSerializer.Serialize(adIds); 
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("PublishBulkAdsAsync Exception: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> UnPublishAdAsync(string adId)
        {
            try
            {
                var url = "api/classified/items/user-ads/unpublish";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var payload = new[] { adId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("UnPublishAdAsync Exception: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> PublishPreLovedAdAsync(string adId)
        {
            try
            {
                var url = "api/classified/preloved/user-ads/publish";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var payload = new[] { adId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("PublishAdAsync Exception: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> UnPublishPreLovedAdAsync(string adId)
        {
            try
            {
                var url = "api/classified/preloved/user-ads/unpublish";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var payload = new[] { adId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("UnPublishAdAsync Exception: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> PublishDealsAdAsync(string adId)
        {
            try
            {
                var url = "api/classified/deals/user-ads/publish";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var payload = new[] { adId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("PublishAdAsync Exception: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> UnPublishDealsAdAsync(string adId)
        {
            try
            {
                var url = "api/classified/deals/user-ads/unpublish";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var payload = new[] { adId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("UnPublishAdAsync Exception: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> PublishCollectiblesAdAsync(string adId)
        {
            try
            {
                var url = "api/classified/collectibles/user-ads/publish";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var payload = new[] { adId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("PublishAdAsync Exception: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> UnPublishCollectiblesAdAsync(string adId)
        {
            try
            {
                var url = "api/classified/collectibles/user-ads/unpublish";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                var payload = new[] { adId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("UnPublishAdAsync Exception: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> RemoveItemAdAsync(string adId)
        {
            try
            {
                var url = $"api/classified/items-ad/{adId}";
                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("RemoveAdAsync Exception: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> RemovePrelovedAsync(string adId)
        {
            try
            {
                var url = $"api/classified/preloved-ad/{adId}";
                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("RemoveAdAsync Exception: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> RemoveCollectiblesAdAsync(string adId)
        {
            try
            {
                var url = $"api/classified/collectibles-ad/{adId}";
                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("RemoveAdAsync Exception: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> RemoveDealsAdAsync(string adId)
        {
            try
            {
                var url = $"api/classified/deals-ad/{adId}";
                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("RemoveAdAsync Exception: " + ex.Message);
                return false;
            }
        }

    }
}
