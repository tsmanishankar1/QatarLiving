using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ToggleTabs;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewStores
{
    public class ViewStoreTableBase : ComponentBase
    {
        [Parameter] public List<CompanyProfileItem> Listings { get; set; } = new();
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Parameter] public int TotalCount { get; set; }
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public int CurrentPage { get; set; }
        [Parameter] public int PageSize { get; set; }
        [Parameter] public EventCallback<string> OnTabChange { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        [Parameter] public EventCallback<CompanyProfileItem> OnViewClicked { get; set; }

        protected string selectedTab = "pendingApproval";

        protected List<ToggleTabs.TabOption> tabOptions =
        [
            new() { Label = "Pending Approval", Value = "pendingApproval" },
            new() { Label = "All", Value = "all" },
            new() { Label = "Need Changes", Value = "needChanges" },
            new() { Label = "Approved", Value = "approved" },
            new() { Label = "Removed", Value = "removed" }
        ];
        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;
            await OnTabChange.InvokeAsync(newTab);
        }
        
        protected void EditStore(CompanyProfileItem order)
        {
            if (!string.IsNullOrEmpty(order?.CompanyName))
            {
                NavigationManager.NavigateTo($"/manage/classified/stores/edit/company/{order.CompanyName}");
            }
        }
    }
}
