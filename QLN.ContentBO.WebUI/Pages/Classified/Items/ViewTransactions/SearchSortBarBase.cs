using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.ViewTransactions
{
    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected NavigationManager NavManager { get; set; } = default!;

        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }

        protected bool ascending = true;
        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;

        // Filters
        protected DateTime? dateCreated;
        protected DateTime? datePublished;
        protected DateTime? dateStart;
        protected DateTime? dateEnd;

        // Popover states
        protected bool showCreatedPopover = false;
        protected bool showPublishedPopover = false;
        protected bool showStartPopover = false;
        protected bool showEndPopover = false;

        // Temp variables
        protected DateTime? tempCreatedDate;
        protected DateTime? tempPublishedDate;
        protected DateTime? tempStartDate;
        protected DateTime? tempEndDate;

        protected async Task OnSearchChanged(ChangeEventArgs e)
        {
            if (e?.Value != null)
                await OnSearch.InvokeAsync(e.Value.ToString());
        }

        protected async Task ToggleSort()
        {
            ascending = !ascending;
            await OnSort.InvokeAsync(ascending);
        }

        protected void ToggleCreatedPopover()
        {
            CloseAllPopovers();
            tempCreatedDate = dateCreated;
            showCreatedPopover = true;
        }

        protected void TogglePublishedPopover()
        {
            CloseAllPopovers();
            tempPublishedDate = datePublished;
            showPublishedPopover = true;
        }

        protected void ToggleStartPopover()
        {
            CloseAllPopovers();
            tempStartDate = dateStart;
            showStartPopover = true;
        }

        protected void ToggleEndPopover()
        {
            CloseAllPopovers();
            tempEndDate = dateEnd;
            showEndPopover = true;
        }

        protected void CancelCreatedPopover() => showCreatedPopover = false;
        protected void CancelPublishedPopover() => showPublishedPopover = false;
        protected void CancelStartPopover() => showStartPopover = false;
        protected void CancelEndPopover() => showEndPopover = false;

        protected void ConfirmCreatedPopover()
        {
            dateCreated = tempCreatedDate;
            showCreatedPopover = false;
        }

        protected void ConfirmPublishedPopover()
        {
            datePublished = tempPublishedDate;
            showPublishedPopover = false;
        }

        protected void ConfirmStartPopover()
        {
            dateStart = tempStartDate;
            showStartPopover = false;
        }

        protected void ConfirmEndPopover()
        {
            dateEnd = tempEndDate;
            showEndPopover = false;
        }

        protected void ClearFilters()
        {
            dateCreated = null;
            datePublished = null;
            dateStart = null;
            dateEnd = null;
        }

        private void CloseAllPopovers()
        {
            showCreatedPopover = false;
            showPublishedPopover = false;
            showStartPopover = false;
            showEndPopover = false;
        }
    }
}
