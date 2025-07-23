using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

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

        public Task<HttpResponseMessage?> DeleteSeasonalPicks()
        {
            throw new NotImplementedException();
        }
    }
}
