using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using System;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.ViewTransactions
{
    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Parameter] public List<ItemViewTransaction> Items { get; set; } = new();
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Parameter] public EventCallback<(DateTime? created, DateTime? published, DateTime? start, DateTime? end)> OnDateFilterChanged { get; set; }
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        protected bool ascending = true;
        protected string searchText = string.Empty;
        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;

        // Date Filters
        protected DateTime? dateCreated;
        protected DateTime? datePublished;
        protected DateTime? dateStart;
        protected DateTime? dateEnd;

        // Popover visibility
        protected bool showCreatedPopover = false;
        protected bool showPublishedPopover = false;
        protected bool showStartPopover = false;
        protected bool showEndPopover = false;

        // Temp values for popovers
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

        protected async void ConfirmCreatedPopover()
        {
            dateCreated = tempCreatedDate;
            showCreatedPopover = false;
            await NotifyDateFilterChanged();
        }

        protected async void ConfirmPublishedPopover()
        {
            datePublished = tempPublishedDate;
            showPublishedPopover = false;
            await NotifyDateFilterChanged();
        }

        protected async void ConfirmStartPopover()
        {
            dateStart = tempStartDate;
            showStartPopover = false;
            await NotifyDateFilterChanged();
        }

        protected async void ConfirmEndPopover()
        {
            dateEnd = tempEndDate;
            showEndPopover = false;
            await NotifyDateFilterChanged();
        }

        protected async void ClearFilters()
        {
            dateCreated = null;
            datePublished = null;
            dateStart = null;
            dateEnd = null;
            searchText = string.Empty;
            await NotifyDateFilterChanged();
        }

        protected async Task NotifyDateFilterChanged()
        {
            if (OnDateFilterChanged.HasDelegate)
            {
                await OnDateFilterChanged.InvokeAsync((dateCreated, datePublished, dateStart, dateEnd));
            }
        }

        private void CloseAllPopovers()
        {
            showCreatedPopover = false;
            showPublishedPopover = false;
            showStartPopover = false;
            showEndPopover = false;
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
                ["Ad ID"] = x.AdId,
                ["Order ID"] = x.OrderId,
                ["Username"] = x.Username,
                ["User Email"] = x.UserEmail,
                ["Transaction Type"] = x.TransactionType,
                ["Product Type"] = x.ProductType,
                ["Category"] = x.Category,
                ["Status"] = x.Status,
                ["Email"] = x.Email,
                ["Mobile"] = x.Mobile,
                ["WhatsApp"] = x.Whatsapp,
                ["Account"] = x.Account,
                ["Amount"] = x.Amount,
                ["Creation Date"] = FormatDate(x.CreationDate),
                ["Published Date"] = FormatDate(x.PublishedDate),
                ["Start Date"] = FormatDate(x.StartDate),
                ["End Date"] = FormatDate(x.EndDate),
                ["Payment Method"] = x.PaymentMethod,
                ["Description"] = x.Description
            }).ToList();

            await JS.InvokeVoidAsync("exportToExcel", exportData, "Classified_Items_ViewTransactions.xlsx", "Transactions");

            Snackbar.Add("Export successful!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
    }

    private string FormatDate(string? raw)
    {
        return DateTime.TryParse(raw, out var date) ? date.ToString("yyyy/MM/dd") : "-";
    }


    }
}
