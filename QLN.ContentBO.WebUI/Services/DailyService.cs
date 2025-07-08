using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;

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

        public async Task<List<EventDTO>> GetFeaturedEventsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EventDTO>>("/api/v2/dailyliving/featuredevents")
                    ?? new List<EventDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Featured Events articles.");
                return new List<EventDTO>();
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
        public async Task<List<DailyTopic>> GetActiveTopicsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<DailyTopic>>("api/v2/dailyliving/dailytopics")
                    ?? new List<DailyTopic>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Featured Events articles.");
                return new List<DailyTopic>();
            }
        }
        public async Task<HttpResponseMessage> UpdateTopicAsync(DailyTopic topic)
        {
            try
            {
                var json = JsonSerializer.Serialize(topic, new JsonSerializerOptions { WriteIndented = true });
                var request = new HttpRequestMessage(HttpMethod.Put, "api/v2/dailyliving/publishstatus")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateTopic");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> DeleteArticleAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/v2/dailyliving/topic/content/{id}");
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting article with ID {Id}", id);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> ReorderFeaturedSlots(IEnumerable<object> slotAssignments, string userId)
{
    try
    {
        var payload = new
        {
            slotAssignments = slotAssignments,
            userId = userId
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/dailyliving/topic/content/reorder")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        return response;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "ReorderDailyTopics");
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }
}


    }
}
