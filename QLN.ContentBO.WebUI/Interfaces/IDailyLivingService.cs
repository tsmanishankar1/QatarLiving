using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IDailyLivingService
    {
        Task<List<DailyLivingArticleDto>> GetTopSectionAsync();
        Task<List<DailyLivingArticleDto>> GetFeaturedEventsAsync();
        Task<List<DailyLivingArticleDto>> GetContentByTopicIdAsync(string topicId);
    }
}
