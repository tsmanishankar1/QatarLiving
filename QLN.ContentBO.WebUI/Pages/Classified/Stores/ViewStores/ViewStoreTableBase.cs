using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using System.Collections.Generic;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewStores
{
    public class ViewStoreTableBase : ComponentBase
    {
        [Parameter] public List<ViewStoreList> Listings { get; set; } = new();
        [Parameter] public int TotalCount { get; set; }
        [Parameter] public int CurrentPage { get; set; }
        [Parameter] public int PageSize { get; set; }

        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        [Parameter] public EventCallback<ViewStoreList> OnViewClicked { get; set; }
    }
}
