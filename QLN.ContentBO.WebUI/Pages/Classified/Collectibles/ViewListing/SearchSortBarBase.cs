using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using Microsoft.JSInterop;


namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.ViewListing
{
    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Parameter] public List<CollectibleItem> Items { get; set; } = new();
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Parameter] public EventCallback<(DateTime? created, DateTime? published)> OnDateFilterChanged { get; set; }
        [Parameter] public EventCallback OnClearFilters { get; set; }
        [Parameter] public EventCallback OnAddClicked { get; set; }
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
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


        protected async void ConfirmCreatedPopover()
        {
            dateCreated = tempCreatedDate;
            showCreatedPopover = false;
            await OnDateFilterChanged.InvokeAsync((dateCreated, datePublished));
        }
        protected async void CancelCreatedPopover()
        {
            dateCreated = null;
            showCreatedPopover = false;
            await OnDateFilterChanged.InvokeAsync((dateCreated, datePublished));
        }

        protected async void ConfirmPublishedPopover()
        {
            datePublished = tempPublishedDate;
            showPublishedPopover = false;
            await OnDateFilterChanged.InvokeAsync((dateCreated, datePublished));
        }
        protected async void CancelPublishedPopover()
        {
            datePublished = null;
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
        protected void ClearDateFilters()
        {
            dateCreated = null;
            datePublished = null;
        }

       protected async Task ShowConfirmationExport()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Export Classified Collectibles" },
                { "Descrption", "Do you want to export the current classified collectibles data to Excel?" },
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

                // Prepare export data with capitalized headers
                var exportData = Items.Select(x => new Dictionary<string, object?>
                {
                    ["Image URL"] = x.Images?.FirstOrDefault()?.Url ?? "-",
                    ["Ad ID"] = x.Id,
                    ["Ad Type"] = (AdTypeEnum)x.AdType,
                    ["Ad Title"] = x.Title,
                    ["User ID"] = x.UserId,
                    ["User Name"] = x.UserName,
                    ["Category"] = x.Category,
                    ["Sub Category"] = x.L2Category,
                    ["Section"] = x.L1Category,
                    ["Created At"] = x.CreatedAt?.ToString("yyyy/MM/dd") ?? "-",
                    ["Published Date"] = x.PublishedDate?.ToString("yyyy/MM/dd") ?? "-",
                    ["Expiry Date"] = x.ExpiryDate?.ToString("yyyy/MM/dd") ?? "-"
                }).ToList();

                await JS.InvokeVoidAsync("exportToExcel", exportData, "Classified_Collectibles_Listing.xlsx", "Classified Collectibles");

                Snackbar.Add("Export successful!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
            }
        }

    }
}