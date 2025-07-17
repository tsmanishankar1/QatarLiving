using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class BannerService : ServiceBase<BannerService>, IBannerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BannerService> _logger;

        public BannerService(HttpClient httpClient, ILogger<BannerService> logger)
            : base(httpClient, logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> CreateBanner(Banner banner)
        {
            try
            {
                var bannerJson = new StringContent(JsonSerializer.Serialize(banner), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/banner/create")
                {
                    Content = bannerJson
                };

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CreateBanner");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> DeleteBanner(Guid bannerId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v2/banner/delete?bannerId={bannerId}");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteBanner");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> GetBannerById(Guid bannerId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/banner/getbyid/{bannerId}");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetBannerById");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> GetBannerTypes()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/banner/getall");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetBannerTypes");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> UpdateBanner(Banner banner)
        {
            try
            {
                var bannerJson = new StringContent(JsonSerializer.Serialize(banner), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/banner/edit")
                {
                    Content = bannerJson
                };

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateBanner");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
