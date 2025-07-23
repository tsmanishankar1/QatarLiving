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
        public async Task<HttpResponseMessage> CreateBanner(BannerDTO banner)
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
        public async Task<HttpResponseMessage> UpdateBanner(BannerDTO banner)
        {
            try
            {
                var bannerJson = new StringContent(JsonSerializer.Serialize(banner), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/banner/edit")
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
        public async Task<HttpResponseMessage> GetBannerByVerticalAndStatus(int? verticalId, bool? status)
        {
            try
            {
                var queryParams = new List<string>();

                if (verticalId.HasValue)
                    queryParams.Add($"verticalId={verticalId.Value}");

                if (status.HasValue)
                    queryParams.Add($"status={status.Value.ToString().ToLower()}");
                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
                var requestUrl = $"/api/v2/banner/getbyverticalandstatus{queryString}";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetBannerByVerticalAndStatus");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> ReorderBanner(List<string> newOrder, int verticalId, int subVerticalId, string pageId)
        {
            try
            {
                var json = JsonSerializer.Serialize(newOrder);
                var url = $"api/v2/banner/reorder?verticalId={verticalId}&subVerticalId={subVerticalId}&pageId={pageId}";
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ReorderBanner");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

    


        
    }
}
