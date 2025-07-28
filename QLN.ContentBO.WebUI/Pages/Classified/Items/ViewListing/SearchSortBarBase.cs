using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.ViewListing
{

    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;

        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Parameter] public EventCallback<(DateTime? created, DateTime? published)> OnDateFilterChanged { get; set; }
        [Parameter] public EventCallback OnClearFilters { get; set; }

        protected bool ascending = true;
        protected string searchText = string.Empty;

        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;

        protected DateTime? dateCreated;
        protected DateTime? datePublished;

        protected bool showCreatedPopover = false;
        protected bool showPublishedPopover = false;

        protected DateTime? tempCreatedDate;
        protected DateTime? tempPublishedDate;

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
            showPublishedPopover = false;
            tempCreatedDate = dateCreated;
            showCreatedPopover = true;
        }

        protected void TogglePublishedPopover()
        {
            showCreatedPopover = false;
            tempPublishedDate = datePublished;
            showPublishedPopover = true;
        }

        protected void CancelCreatedPopover() => showCreatedPopover = false;
        protected void CancelPublishedPopover() => showPublishedPopover = false;

        protected async void ConfirmCreatedPopover()
        {
            dateCreated = tempCreatedDate;
            showCreatedPopover = false;
            await OnDateFilterChanged.InvokeAsync((dateCreated, datePublished));
        }

        protected async void ConfirmPublishedPopover()
        {
            datePublished = tempPublishedDate;
            showPublishedPopover = false;
            await OnDateFilterChanged.InvokeAsync((dateCreated, datePublished));
        }

        protected async void ClearFilters()
        {
            dateCreated = null;
            datePublished = null;
            ascending = true;
            searchText = string.Empty;
            await OnClearFilters.InvokeAsync();
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

        protected void ClearDateFilters()
        {
            dateCreated = null;
            datePublished = null;
        }

    }
}