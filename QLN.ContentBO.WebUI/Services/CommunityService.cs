using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class CommunityService : ServiceBase<CommunityService>, ICommunityService
    {
        private readonly HttpClient _httpClient;

        public CommunityService(HttpClient httpClient, ILogger<CommunityService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> GetAllCommunity()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/event/getAllCommunity");
                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in GetAllCommunity");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> DeleteCommunity(int id)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v2/event/deleteCommunity/{id}");
                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in DeleteCommunity");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetAllCommunitySearch(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/event/getAllCommunitySearch")
                {
                    Content = content
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in GetAllCommunitySearch");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
