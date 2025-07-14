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
        public async Task<HttpResponseMessage> ReorderFeaturedSlots(DailySlotAssignmentRequest slotAssignments)
        {
            try
            {
                var json = JsonSerializer.Serialize(slotAssignments, new JsonSerializerOptions { WriteIndented = true });

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
        public async Task<HttpResponseMessage> GetAvailableArticles(string topicId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/dailyliving/topic/{topicId}/unusedarticles");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventById");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> AddArticle(DailyLivingArticleDto article)
        {
            try
            {
                object payload;
                if (article.ContentType == 3)
                {
                    payload = new
                    {
                        contentType = article.ContentType,
                        topicId = article.TopicId,
                        contentUrl = article.ContentURL
                    };
                }
                else
                {
                    payload = new
                    {
                        slotType = article.SlotType == 0 ? 1 : article.SlotType,
                        title = string.IsNullOrWhiteSpace(article.Title) ? "unknown" : article.Title,
                        category = string.IsNullOrWhiteSpace(article.Category) ? "unknown" : article.Category,
                        subcategory = string.IsNullOrWhiteSpace(article.Subcategory) ? "unknown" : article.Subcategory,
                        relatedContentId = string.IsNullOrWhiteSpace(article.Id) ? Guid.NewGuid().ToString() : article.Id,
                        contentType = article.ContentType == 0 ? 1 : article.ContentType,
                        publishedDate = article.PublishedDate == default ? DateTime.UtcNow : article.PublishedDate,
                        endDate = article.EndDate ?? DateTime.UtcNow.AddDays(7),
                        createdBy = string.IsNullOrWhiteSpace(article.CreatedBy) ? "unknown" : article.CreatedBy,
                        createdAt = article.CreatedAt == default ? DateTime.UtcNow : article.CreatedAt,
                        updatedBy = string.IsNullOrWhiteSpace(article.UpdatedBy) ? "unknown" : article.UpdatedBy,
                        updatedAt = article.UpdatedAt ?? DateTime.UtcNow,
                        topicId = article.TopicId
                    };
                }
                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/dailyliving/topic/content")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CreateArticle");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> ReplaceArticle(DailyLivingArticleDto article)
        {
            try
            {
                object payload;
                if (article.ContentType == 3)
                {
                    payload = new
                    {
                        contentType = article.ContentType,
                        topicId = article.TopicId,
                        slotNumber = article.SlotNumber,
                        contentUrl = article.ContentURL
                    };
                }
                else
                {
                    payload = new
                    {
                        slotType = article.SlotType,
                        title = string.IsNullOrWhiteSpace(article.Title) ? "unknown" : article.Title,
                        category = string.IsNullOrWhiteSpace(article.Category) ? "unknown" : article.Category,
                        subcategory = string.IsNullOrWhiteSpace(article.Subcategory) ? "unknown" : article.Subcategory,
                        relatedContentId = string.IsNullOrWhiteSpace(article.Id) ? Guid.NewGuid().ToString() : article.Id,
                        contentType = article.ContentType,
                        publishedDate = article.PublishedDate == default ? DateTime.UtcNow : article.PublishedDate,
                        endDate = article.EndDate ?? DateTime.UtcNow.AddDays(7),
                        slotNumber = article.SlotNumber,
                        createdBy = string.IsNullOrWhiteSpace(article.CreatedBy) ? "unknown" : article.CreatedBy,
                        createdAt = article.CreatedAt == default ? DateTime.UtcNow : article.CreatedAt,
                        updatedBy = string.IsNullOrWhiteSpace(article.UpdatedBy) ? "unknown" : article.UpdatedBy,
                        updatedAt = article.UpdatedAt ?? DateTime.UtcNow,
                        topicId = article.TopicId
                    };
                }
                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/dailyliving/topic/content")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CreateArticle");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> ReplaceTopSectionArticle(DailyLivingArticleDto article)
        {
            try
            {
                var payload = new
                {
                    id = string.IsNullOrWhiteSpace(article.RelatedContentId) ? Guid.NewGuid().ToString() : article.RelatedContentId,
                    slotType = article.SlotType,
                    title = article.Title,
                    category = string.IsNullOrWhiteSpace(article.Category) ? "unknown" : article.Category,
                    subcategory = string.IsNullOrWhiteSpace(article.Subcategory) ? "unknown" : article.Subcategory,
                    relatedContentId = string.IsNullOrWhiteSpace(article.Id) ? Guid.NewGuid().ToString() : article.Id,
                    contentType = article.ContentType,
                    publishedDate = article.PublishedDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    endDate = (article.EndDate ?? DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    slotNumber = article.SlotNumber,
                    createdBy = article.CreatedBy.ToString(),
                    createdAt = article.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    updatedBy = article.UpdatedBy.ToString(),
                    updatedAt = article.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/dailyliving/topsection")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CreateArticle");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetAvailableTopSectionArticles()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/dailyliving/topsection/unusedarticles");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetUnusedTopSectionArticles");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
