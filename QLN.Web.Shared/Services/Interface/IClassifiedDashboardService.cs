using static QLN.Web.Shared.Pages.Subscription.SubscriptionDetails;

namespace QLN.Web.Shared.Services.Interface
{
    public interface IClassifiedDashboardService
    {
        Task<ItemDashboardResponse?> GetItemDashboard(string authToken);

    }
}
