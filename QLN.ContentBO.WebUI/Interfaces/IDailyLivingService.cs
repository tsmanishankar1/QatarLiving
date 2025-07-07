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
    }
}
