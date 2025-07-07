using QLN.Web.Shared.Models;

namespace QLN.Web.Shared.Services.Interface
{
    public interface IQLAnalyticsService
    {
        Task TrackEventAsync(QLAnalyticsCallProps props, string browserId, string sessionId);
        Task TrackEventFromClientAsync(QLAnalyticsCallProps props, string browserId, string sessionId);
        Task TrackEventForContentAsync(ContentEventAnalyticsData data, string browserId, string sessionId, string? token);
        Task TrackEventForJobsAsync(JobEventAnalyticsData data, string browserId, string sessionId, string? token);
        Task TrackEventForPropertiesAsync(PropertyEventAnalyticsData data, string browserId, string sessionId, string? token);
        Task TrackEventForRewardsAsync(RewardEventAnalyticsData data, string browserId, string sessionId, string? token);
        Task TrackEventForVehiclesAsync(VehicleEventAnalyticsData data, string browserId, string sessionId, string? token);
    }
}
