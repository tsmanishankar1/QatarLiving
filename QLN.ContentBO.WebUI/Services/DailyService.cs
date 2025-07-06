using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class DailyService : ServiceBase<DailyService>, IDailyLivingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DailyService> _logger;

        public DailyService(HttpClient httpClient, ILogger<DailyService> logger)
            : base(httpClient, logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<DailyLivingArticleDto>> GetTopSectionAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<DailyLivingArticleDto>>("/api/v2/dailyliving/topsection")
                    ?? new List<DailyLivingArticleDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Top Section articles.");
                return new List<DailyLivingArticleDto>();
            }
        }

        public async Task<List<DailyLivingArticleDto>> GetFeaturedEventsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<DailyLivingArticleDto>>("/api/v2/dailyliving/featuredevents")
                    ?? new List<DailyLivingArticleDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Featured Events articles.");
                return new List<DailyLivingArticleDto>();
            }
        }

        public async Task<List<DailyLivingArticleDto>> GetContentByTopicIdAsync(string topicId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<DailyLivingArticleDto>>($"/api/v2/dailyliving/topic/content?topicId={topicId}")
                    ?? new List<DailyLivingArticleDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Topic Content articles for TopicId: {TopicId}", topicId);
                return new List<DailyLivingArticleDto>();
            }
        }
    }
}
