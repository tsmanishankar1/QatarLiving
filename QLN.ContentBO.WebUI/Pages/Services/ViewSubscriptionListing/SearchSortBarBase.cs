using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;

namespace QLN.ContentBO.WebUI.Pages.Services.ViewSubscriptionListing
{

    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        protected string searchText = string.Empty;
        [Parameter] public EventCallback<string> OnTypeChange { get; set; }
        [Parameter] public EventCallback<(DateTime? startDate, DateTime? endDate)> OnDateFilterChanged { get; set; }
        [Parameter] public EventCallback OnClearFilters { get; set; }
        protected bool ascending = true;
        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;
        protected DateTime? dateCreated;
        protected DateTime? datePublished;
        protected DateTime? tempCreatedDate;
        protected DateTime? tempPublishedDate;
        protected DateRange _dateRange = new();
        protected List<string> SubscriptionTypes = new()
        {
            "1 Months",
            "2 Months",
            "3 Months",
            "4 Months",
            "5 Months",
            "6 Months"
        };
        protected string SelectedSubscriptionType { get; set; } = null;
        protected bool showDatePopover = false;
        protected DateRange? _tempDateRange = new();
         protected void CancelDatePopover()
        {
            showDatePopover = false;
        }
        protected async Task ApplyDatePopover()
        {
            _dateRange = new DateRange(_tempDateRange.Start, _tempDateRange.End);
            dateCreated = _tempDateRange.Start;
            datePublished = _tempDateRange.End;
            showDatePopover = false;
            await OnDateFilterChanged.InvokeAsync((dateCreated, datePublished));
            StateHasChanged();
        }
        protected void ToggleDatePopover()
        {
            _tempDateRange = new DateRange(_dateRange.Start, _dateRange.End);
            showDatePopover = !showDatePopover;
        }
        protected async Task OnSearchChanged(ChangeEventArgs e)
        {
            if (e?.Value != null)
                await OnSearch.InvokeAsync(e.Value.ToString());
        }
        protected async Task OnSubscriptionChanged(string selected)
        {
            SelectedSubscriptionType = selected;
            if (SelectedSubscriptionType != null)
                await OnTypeChange.InvokeAsync(SelectedSubscriptionType);
        }
        protected async Task ToggleSort()
        {
            ascending = !ascending;
            await OnSort.InvokeAsync(ascending);
        }
        protected async Task ClearFilters()
        {
            dateCreated = null;
            datePublished = null;
            _tempDateRange = null;
            tempCreatedDate = null;
            tempPublishedDate = null;
            SelectedSubscriptionType = null;
            searchText = null;
            await OnClearFilters.InvokeAsync();
            StateHasChanged();
        }
    }
}