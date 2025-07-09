using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IDailyLivingService
    {
        Task<List<DailyLivingArticleDto>> GetTopSectionAsync();
        Task<List<EventDTO>> GetFeaturedEventsAsync();
        Task<List<DailyLivingArticleDto>> GetContentByTopicIdAsync(string topicId);
        Task<List<DailyTopic>> GetActiveTopicsAsync();
        Task<HttpResponseMessage> UpdateTopicAsync(DailyTopic topic);
        Task<HttpResponseMessage> DeleteArticleAsync(string id);
        Task<HttpResponseMessage> ReorderFeaturedSlots(DailySlotAssignmentRequest request);
        Task<HttpResponseMessage> GetAvailableArticles(string topicId);
        Task<HttpResponseMessage> AddArticle(DailyLivingArticleDto article);
        Task<HttpResponseMessage> ReplaceTopSectionArticle(DailyLivingArticleDto article);
        Task<HttpResponseMessage> ReplaceArticle(DailyLivingArticleDto article);
    }
}
