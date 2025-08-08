using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class StoresService : ServiceBase<StoresService>, IStoresService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public StoresService(HttpClient httpClient, ILogger<StoresService> Logger)
           : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }

        public async Task<HttpResponseMessage> GetAllStoresListing(CompanyRequestPayload companyRequestPayload)
        {
            try
            {
                var json = JsonSerializer.Serialize(companyRequestPayload);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/companyprofile/getallcompanies")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllStoresListing");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public Task<HttpResponseMessage> GetAllStoresSubscription(FilterRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetStoresById(string vertical, string adId)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> UpdateStoresSubscription(int orderId, string status)
        {
            throw new NotImplementedException();
        }
    }
}
