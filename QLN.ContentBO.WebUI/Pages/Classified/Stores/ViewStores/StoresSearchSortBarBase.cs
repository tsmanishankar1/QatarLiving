using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewStores
{

    public class StoresSearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;

        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }

        protected bool ascending = true;
        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;
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

  


        // Date range logic
        protected DateRange _dateRange = new(); // both Start and End are null by default
        protected DateRange _tempDateRange = new();

        protected void ClearFilters()
        {
            _dateRange = new();
            _tempDateRange = new();
        }

        protected bool showDatePopover = false;

        protected void ToggleDatePopover()
        {
            _tempDateRange = new DateRange(_dateRange.Start, _dateRange.End);
            showDatePopover = !showDatePopover;
        }

        protected void CancelDatePopover()
        {
            showDatePopover = false;
        }

        protected void ApplyDatePopover()
        {
            _dateRange = new DateRange(_tempDateRange.Start, _tempDateRange.End);
            showDatePopover = false;
            StateHasChanged();
        }

        protected async Task AddEventCallback()
        {
            var parameters = new DialogParameters
        {
            { "Title", "Create Ad" },
            { "Label", "User Email*" },
            { "ButtonText", "Continue" },
            { "ListItems", new List<DropdownItem>
                {
                    new() { Id = 1, Label = "john.doe@hotmail.com" },
                    new() { Id = 2, Label = "jane.doe@gmail.com" },
                    new() { Id = 3, Label = "alice@example.com" },
                    new() { Id = 4, Label = "bob@workmail.com" },
                    new() { Id = 5, Label = "emma@company.com" }
                }
            },
            { "OnSelect", EventCallback.Factory.Create<DropdownItem>(this, HandleSelect) }
        };

            DialogService.Show<AutoSelectDialog>("", parameters);
        }

        protected Task HandleSelect(DropdownItem selected)
        {
            Console.WriteLine($"Selected: {selected.Label}");

            // Option 1: Pass by query string (recommended for readability)
            var targetUrl = $"/manage/classified/items/createform?email={selected.Label}";
            NavManager.NavigateTo(targetUrl);

            return Task.CompletedTask;
        }
        protected List<string> SubscriptionTypes = new()
        {
            "Free",
            "Basic",
            "Pro",
            "Enterprise"
        };

        protected string SelectedSubscriptionType { get; set; } = null;

        protected Task OnSubscriptionChanged(string selected)
        {
            SelectedSubscriptionType = selected;
            // Handle logic like filtering, etc.
            return Task.CompletedTask;
        }

    }
}