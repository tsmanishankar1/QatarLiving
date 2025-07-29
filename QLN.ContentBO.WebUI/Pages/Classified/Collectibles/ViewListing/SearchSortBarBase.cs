using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using Microsoft.JSInterop;
using ClosedXML.Excel;
using System.IO;
using QLN.ContentBO.WebUI.Interfaces;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.ViewListing
{
    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Parameter] public List<ClassifiedItemViewListing> Items { get; set; } = new();
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Parameter] public EventCallback<(DateTime? created, DateTime? published)> OnDateFilterChanged { get; set; }
        [Parameter] public EventCallback OnClearFilters { get; set; }
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IExcelExportService ExcelExportService { get; set; } = default!;

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
             var targetUrl = $"/manage/classified/collectibles/createform?email={selected.Label}";
            NavManager.NavigateTo(targetUrl);

            return Task.CompletedTask;
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
                var columns = new Dictionary<string, Func<ClassifiedItemViewListing, object?>>
                {
                    ["Image URL"] = x => x.Images?.FirstOrDefault()?.Url,
                    ["Ad ID"] = x => x.Id,
                    ["Ad Type"] = x => x.AdType,
                    ["Ad Title"] = x => x.Title,
                    ["User ID"] = x => x.UserId,
                    ["User Name"] = x => x.UserName,
                    ["Category"] = x => x.Category,
                    ["Sub Category"] = x => x.L2Category,
                    ["Section"] = x => x.L1Category,
                    ["Created At"] = x => x.CreatedAt?.ToString("yyyy/MM/dd"),
                    ["Published Date"] = x => x.PublishedDate?.ToString("yyyy/MM/dd"),
                    ["Expiry Date"] = x => x.ExpiryDate?.ToString("yyyy/MM/dd")
                };

                var fileName = await ExcelExportService.ExportAsync(Items, columns, "Classified Collectibles", "Classified_Collectibles_View_Listing");

                await JS.InvokeVoidAsync("triggerFileDownload", $"/exports/{fileName}");
                Snackbar.Add("Export successful!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
            }
        }

    }
}