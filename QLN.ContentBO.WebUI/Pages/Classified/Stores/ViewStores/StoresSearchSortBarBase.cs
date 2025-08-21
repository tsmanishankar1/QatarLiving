using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Components.AutoSelectDialog;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewStores
{

    public class StoresSearchSortBarBase : QLComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; }
        [Parameter] public EventCallback<(DateTime? createdFrom, DateTime? createdTo)> OnDateFilterChanged { get; set; }
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<string> OnTypeChange { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Parameter] public List<CompanyProfileItem> Items { get; set; } = [];
        protected bool ascending = true;
        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                    var tListOfSubsctiptions = await GetSubscriptionProductsAsync((int)VerticalTypeEnum.Classifieds, (int)SubVerticalTypeEnum.Stores);
                    if (tListOfSubsctiptions != null && tListOfSubsctiptions.Count != 0)
                    {
                        SubscriptionTypes = [.. tListOfSubsctiptions.Select(x => x.ProductName)];
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnAfterRenderAsync");
            }
        }

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
        protected DateRange _dateRange = new();
        protected DateRange _tempDateRange = new();
        protected DateTime? dateCreatedFrom;
        protected DateTime? dateCreatedTo;

        protected void ClearFilters()
        {
            _dateRange = new();
            _tempDateRange = new();
        }

        protected bool showDatePopover = false;

        protected void ToggleDatePopover()
        {
            if (_tempDateRange?.Start != null && _tempDateRange?.End != null)
            {
                _tempDateRange = new DateRange(_dateRange.Start, _dateRange.End);
            }
            else
            {
                _tempDateRange = null;
            }

            showDatePopover = !showDatePopover;
        }
        protected void CancelDatePopover()
        {
            showDatePopover = false;
        }

        protected async void ApplyDatePopover()
        {
            _dateRange = new DateRange(_tempDateRange.Start, _tempDateRange.End);
            dateCreatedFrom = _tempDateRange.Start;
            dateCreatedTo = _tempDateRange.End;
            showDatePopover = false;
            await OnDateFilterChanged.InvokeAsync((dateCreatedFrom, dateCreatedTo));
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

        protected async Task OnSubscriptionChanged(string selected)
        {
            SelectedSubscriptionType = selected.ToString() ?? string.Empty;
            await OnSearch.InvokeAsync(SelectedSubscriptionType);
        }

        protected async Task ShowConfirmationExport()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Export Classified Stores" },
                { "Descrption", "Do you want to export the current Stores data to Excel?" },
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
                if (Items == null || Items.Count == 0)
                {
                    Snackbar.Add("No data available to export.", Severity.Warning);
                    return;
                }
                var exportData = Items.Select(x => new Dictionary<string, object?>
                {
                    ["Company Name"] = x.CompanyName,
                    ["Email"] = x.Email,
                    ["Mobile"] = x.PhoneNumber,
                    ["Whatsapp"] = x.WhatsAppNumber,
                    ["Web Url"] = x.WebsiteUrl,
                    ["Status"] = x.Status,
                    ["Start Day"] = x.StartDay,
                    ["End Day"] = x.EndDay,
                }).ToList();

                await JS.InvokeVoidAsync("exportToExcel", exportData, "Classifieds_Stores.xlsx", "Stores");

                Snackbar.Add("Export successful!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
            }
        }
    }
}