using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewSubscriptionListing
{
    public class ViewSubscriptionTableBase : ComponentBase
    {
        [Parameter] public List<StoreSubscriptionItem> Listings { get; set; } = [];
        [Parameter] public int TotalCount { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Parameter] public int CurrentPage { get; set; }
        [Parameter] public int PageSize { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }

        protected void EditOrder(StoreSubscriptionItem storeSubscriptionItem)
        {
            if (!string.IsNullOrEmpty(storeSubscriptionItem?.UserName))
            {
                NavigationManager.NavigateTo($"/manage/classified/stores/edit/company/{storeSubscriptionItem.UserName}");
            }
        }

        protected void CancelOrder(StoreSubscriptionItem order)
        {
            // Logic to cancel the order can go here

        }
    }
}
