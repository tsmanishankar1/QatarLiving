using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using System.Collections.Generic;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewSubscriptionListing
{
    public class ViewSubscriptionTableBase : ComponentBase
    {
        [Parameter] public List<ViewSubscriptionListingDto> Listings { get; set; } = new();
        [Parameter] public int TotalCount { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Parameter] public int CurrentPage { get; set; }
        [Parameter] public int PageSize { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        protected void EditOrder(ViewSubscriptionListingDto order)
    {
        if (!string.IsNullOrEmpty(order?.UserName))
        {
            NavigationManager.NavigateTo($"/manage/classified/stores/edit/compnay/{order.UserName}");
        }
    }

    protected void CancelOrder(ViewSubscriptionListingDto order)
    {
        // Logic to cancel the order can go here

    }
    }
}
