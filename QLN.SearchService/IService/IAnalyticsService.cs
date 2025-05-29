using System.Threading.Tasks;
using QLN.SearchService.Models;

namespace QLN.SearchService.Service
{
    public interface IAnalyticsService
    {
        Task<AnalyticsIndex?> GetAsync(string section, string entityId);
        Task UpsertAsync(AnalyticsEventRequest request);
    }
}
