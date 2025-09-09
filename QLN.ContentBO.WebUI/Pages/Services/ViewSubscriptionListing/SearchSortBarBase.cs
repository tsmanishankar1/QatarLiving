using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;

namespace QLN.ContentBO.WebUI.Pages.Services.ViewSubscriptionListing
{

    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        protected string searchText = string.Empty;
        [Parameter] public List<ServiceAdPaymentSummaryDto> Items { get; set; } = new();
        [Parameter] public EventCallback<string> OnTypeChange { get; set; }
        [Parameter] public EventCallback<(DateTime? startDate, DateTime? endDate)> OnDateFilterChanged { get; set; }
        [Parameter] public EventCallback OnClearFilters { get; set; }
        protected bool ascending = true;
        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;
        protected DateTime? dateCreated;
        protected DateTime? datePublished;
        protected DateTime? tempCreatedDate;
        protected DateTime? tempPublishedDate;
        protected DateRange? _dateRange = new();
        protected List<string> SubscriptionTypes = new()
        {
             "1 Month Pay2 Publish",
            "Services - 1 Month Pay2 Publish",
            "Services - Pay2 Promote 1 Month",
            "Services - Pay2 Feature 1 Month",
            "Services - Hero Banner",
            "Services - Mid Page Banner",
            "Services - Hero Banner -Search Page",
            "Services - Search results Card",
            "Services - Hero banner - Detailed Page",
            "Services - Details page Card",
            "Services - Details Page Side Banner",
            "Services 1 month",
            "Services 1 month Plus",
            "Services 3 month",
            "Services 3 month Plus",
            "Services 6 month",
            "Services 6 month Plus",
            "Services 12 month",
            "Services 12 month Plus"
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
            _dateRange = null;
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
        protected async Task ShowConfirmationExport()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Export Classified Items" },
                { "Descrption", "Do you want to export the current Subscription Listing data to Excel?" },
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
                    ["Ad ID"] = x.AddId,
                    ["Order ID"] = x.OrderId,
                    ["Subscription Plan"] = x.SubscriptionPlan,
                    ["Username"] = x.UserName,
                    ["Email"] = x.EmailAddress, 
                    ["Mobile"] = x.Mobile,
                    ["Whatsapp"] = x.WhatsappNumber,
                    ["Amount"] = x.Amount,
                    ["Status"] = x.Status?.ToString(),
                    ["Start Date"] = x.StartDate,
                    ["End Date"] = x.EndDate,
                }).ToList();

                await JS.InvokeVoidAsync("exportToExcel", exportData, "Services_SubscriptionListings.xlsx", "Subscription");

                Snackbar.Add("Export successful!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
            }
        }


    }
}