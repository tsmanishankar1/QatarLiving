using QLN.SearchService.Models;

namespace QLN.SearchService.IRepository
{
    public interface IAnalyticsRepository
    {
        Task<AnalyticsIndex?> GetByKeyAsync(string key);
        Task UpsertAsync(AnalyticsIndex item);
    }
}
