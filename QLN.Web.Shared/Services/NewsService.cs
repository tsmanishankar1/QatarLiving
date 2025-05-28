using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Web.Shared.Services.Interface;
using System.Net;

namespace QLN.Web.Shared.Services
{
    public class NewsService : INewsService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public NewsService(HttpClient httpClient, IOptions<ApiSettings> options)
        {
            _httpClient = httpClient;
            _baseUrl = options.Value.BaseUrl.TrimEnd('/');
        }

        public async Task<HttpResponseMessage?> GetNewsCommunityAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/content/qln_news_news_community/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsHealthAndEducationAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/content/qln_news_news_health_education/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsLawAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/content/qln_news_news_law/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsMiddleEastAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/content/qln_news_news_middle_east/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsQatarAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/content/qln_news_news_qatar/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsQatarAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetNewsWorldAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/content/qln_news_news_world/landing");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNewsQatarAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
