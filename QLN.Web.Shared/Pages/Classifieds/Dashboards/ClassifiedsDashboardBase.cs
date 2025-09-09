using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Pages.Classifieds.Dashboards
{
    public class ClassifiedsDashboardBase : ComponentBase
    {
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        [Parameter] public DashboardType SelectedDashboard { get; set; } = DashboardType.Items;

        protected List<DashboardType> DashboardOptions => Enum.GetValues(typeof(DashboardType)).Cast<DashboardType>().ToList();

        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new() { Label = "Classifieds", Url = "/qln/classifieds" },
                new() { Label = "Dashboard", Url = $"/qln/classified/dashboard/{SelectedDashboard.ToString().ToLower()}", IsLast = true }
            };
        }

        protected void SwitchDashboard(DashboardType newDashboard)
        {
            if (SelectedDashboard != newDashboard)
                NavigationManager.NavigateTo($"/qln/classified/dashboard/{newDashboard.ToString().ToLower()}");
        }

        protected string GetDashboardName(DashboardType dashboard)
        {
            var member = typeof(DashboardType).GetMember(dashboard.ToString()).FirstOrDefault();
            var displayAttr = member?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            return displayAttr?.Name ?? dashboard.ToString();
        }
    }

    public enum DashboardType
    {
        [Display(Name = "Items")] Items = 1,
        [Display(Name = "Deals")] Deals = 2,
        [Display(Name = "Stores")] Stores = 3,
        [Display(Name = "PreLoved")] PreLoved = 4,
        [Display(Name = "Collectibles")] Collectibles = 5
    }
}
