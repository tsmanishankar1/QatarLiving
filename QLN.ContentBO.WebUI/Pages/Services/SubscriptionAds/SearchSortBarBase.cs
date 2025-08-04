using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;

namespace QLN.ContentBO.WebUI.Pages.Services.SubscriptionAds
{

    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Parameter] public List<ServiceSubscriptionAdSummaryDto> Items { get; set; } = new();
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Parameter] public EventCallback<(DateTime? created, DateTime? published)> OnDateFilterChanged { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
         [Inject] protected IJSRuntime JS { get; set; } = default!;
        protected string searchText = string.Empty;
        protected bool ascending = true;
        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;

        protected DateTime? dateCreated;
        protected DateTime? datePublished;
        [Parameter] public EventCallback OnClearFilters { get; set; }
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
        protected async Task ShowConfirmationExport()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Export Classified Items" },
                { "Descrption", "Do you want to export the current classified item view transactions data to Excel?" },
                { "ButtonTitle", "Export" },
                { "OnConfirmed", EventCallback.Factory.Create(this, ExportToExcel) }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;
        }
        private async Task ExportToExcel()
    {
        try
        {
            if (Items == null || !Items.Any())
            {
                Snackbar.Add("No data available to export.", Severity.Warning);
                return;
            }
                var exportData = Items.Select(x => new Dictionary<string, object?>
                {
                    ["Ad ID"] = x.Id,
                    ["User ID"] = x.UserId,
                    ["Ad Title"] = x.AdTitle,
                    ["Username"] = x.UserName, 
                    ["Username"] = x.UserName,
                    ["Category"] = x.Category, 
                    ["Sub Category"] = x.SubCategory,
                    ["Status"] = x.Status?.ToString(),
                    ["Creation Date"] = x.CreationDate,
                    ["Date Published"] = x.DatePublished,
                    ["Date Expiry"] = x.DateExpiry,
                    ["Favourites"] = x.Favorites,
                }).ToList();

            await JS.InvokeVoidAsync("exportToExcel", exportData, "Services_SubscriptionAds.xlsx", "Transactions");

            Snackbar.Add("Export successful!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
    }



        protected Task HandleSelect(DropdownItem selected)
        {

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