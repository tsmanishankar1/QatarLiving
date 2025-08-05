using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;

namespace QLN.ContentBO.WebUI.Pages.Services.VerifiedSellerRequest
{

    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Parameter] public List<VerificationProfileStatus> Items { get; set; } = new();
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] protected IJSRuntime JS { get; set; } = default!;

        protected bool ascending = true;
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

        protected void ClearFilters()
        {
            dateCreated = null;
            datePublished = null;
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
                    ["Business Name"] = x.BusinessName,
                    ["User Name"] = x.Username,
                    ["CR file"] = x.CRFile,
                    ["CR License"] = x.CRLicense, 
                    ["End Date"] = x.Enddate,
                }).ToList();

            await JS.InvokeVoidAsync("exportToExcel", exportData, "Services_VerifiedSellerRequests.xlsx", "Transactions");

            Snackbar.Add("Export successful!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
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