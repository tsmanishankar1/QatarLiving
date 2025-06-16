using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.BreadCrumb;

namespace QLN.Web.Shared.Pages.Classifieds.Dashboards
{
    public class ItemDashboardBase : ComponentBase
    {

        [Inject] private NavigationManager Navigation { get; set; } = default!;

        protected List<BreadcrumbItem> breadcrumbItems = new();

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new() { Label = "Classifieds", Url = "qln/classifieds" },
                new() { Label = "Dashboard", Url = "/qln/classified/dashboard/items", IsLast = true }
            };
        }
    }
}
