using QLN.Web.Shared.Models;
using static QLN.Web.Shared.Pages.Subscription.SubscriptionDetails;

namespace QLN.Web.Shared.Services.Interface
{
    public interface IClassifiedDashboardService
    {
        Task<ItemDashboardResponse?> GetItemDashboard(string authToken);
        Task<ItemDashboardResponse?> GetPreLovedDashboard(string authToken);

    }
}
