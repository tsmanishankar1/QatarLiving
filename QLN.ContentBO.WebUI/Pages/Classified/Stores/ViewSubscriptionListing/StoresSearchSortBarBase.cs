using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewSubscriptionListing
{

    public class StoresSearchSortBarBase : QLComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Parameter] public List<StoreSubscriptionItem> Items { get; set; } = new();

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                    var tListOfSubsctiptions = await GetSubscriptionProductsAsync((int)VerticalTypeEnum.Classifieds, (int)SubVerticalTypeEnum.Stores);
                    if (tListOfSubsctiptions != null && tListOfSubsctiptions.Count != 0)
                    {
                        SubscriptionTypes = [.. tListOfSubsctiptions.Select(x => x.ProductName).ToList()];
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnAfterRenderAsync");
            }
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

           await DialogService.ShowAsync<AutoSelectDialog>("", parameters);
        }

        protected Task HandleSelect(DropdownItem selected)
        {
            Console.WriteLine($"Selected: {selected.Label}");

            // Option 1: Pass by query string (recommended for readability)
            var targetUrl = $"/manage/classified/items/createform?email={selected.Label}";
            NavManager.NavigateTo(targetUrl);

            return Task.CompletedTask;
        }
        protected List<string> SubscriptionTypes = [];

        protected string SelectedSubscriptionType { get; set; } = null;

        protected Task OnSubscriptionChanged(string selected)
        {
            SelectedSubscriptionType = selected;
            return Task.CompletedTask;
        }
        protected async Task ShowConfirmationExport()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Export Classified Subscriptions" },
                { "Descrption", "Do you want to export the current Subscription data to Excel?" },
                { "ButtonTitle", "Export" },
                { "OnConfirmed", EventCallback.Factory.Create(this, ExportToExcel) }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<ConfirmationDialog>("", parameters, options);
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
                    ["Order Id"] = x.OrderId,
                    ["Subscripton Type"] = x.SubscriptionType,
                    ["User Name"] = x.UserName,
                    ["Email"] = x.Email,
                    ["Mobile"] = x.Mobile,
                    ["Whatsapp"] = x.Whatsapp,
                    ["Web Url"] = x.WebUrl,
                    ["Amount"] = x.Amount,
                    ["Status"] = x.Status,
                    ["Start Date"] = x.StartDate.ToString("dd-MM-yyyy hh:mmtt"),
                    ["End Date"] = x.EndDate.ToString("dd-MM-yyyy hh:mmtt"),
                    ["Web Leads"] = x.WebLeads,
                    ["Email Leads"] = x.EmailLeads,
                    ["WhatsApp Leads"] = x.WhatsappLeads,
                    ["Phone Leads"] = x.PhoneLeads,
                }).ToList();

                await JS.InvokeVoidAsync("exportToExcel", exportData, "Classifieds_StoresSubscriptions.xlsx", "Subscriptions");

                Snackbar.Add("Export successful!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
            }
        }
    }
}