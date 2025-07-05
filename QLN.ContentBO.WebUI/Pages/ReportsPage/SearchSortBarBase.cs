using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.ReportsPage
{
    public class SearchSortBarBase : ComponentBase
    {
         [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }

        protected bool ascending = true;
        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;


        protected async Task OnSearchChanged(ChangeEventArgs e)
    {
        if (e?.Value != null)
        {
            await OnSearch.InvokeAsync(e.Value.ToString());
        }
    }

    protected async Task ToggleSort()
    {
        ascending = !ascending;
        await OnSort.InvokeAsync(ascending);
    }
    }
}