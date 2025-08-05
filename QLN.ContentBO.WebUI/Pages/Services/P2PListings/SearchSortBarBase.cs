using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
namespace QLN.ContentBO.WebUI.Pages.Services.P2PListings
{

    public class SearchSortBarBase : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavManager { get; set; } = default!;
        [Parameter] public List<ServiceAdSummaryDto> Items { get; set; } = new();
        [Inject] ISnackbar Snackbar { get; set; }
        [Parameter] public EventCallback<string> OnSearch { get; set; }
        [Parameter] public EventCallback<bool> OnSort { get; set; }
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Parameter] public EventCallback<(DateTime? createdFrom,DateTime? createdTo ,DateTime? publishedFrom,DateTime? publishedTo)> OnDateFilterChanged { get; set; }
        protected string searchText = string.Empty;
        protected DateRange _createDateRange = new();
        protected DateRange _publishDateRange = new();
        protected DateRange _createTempDateRange = new();
        protected DateRange _publishTempDateRange = new();

        protected bool ascending = true;
        protected string SortIcon => ascending ? Icons.Material.Filled.FilterList : Icons.Material.Filled.FilterListOff;

        protected DateTime? dateCreatedFrom;
        protected DateTime? dateCreatedTo;
        protected DateTime? datePublishedFrom;
        protected DateTime? datePublishedTo;
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
           if (_createDateRange?.Start != null && _createDateRange?.End != null)
            {
                _createTempDateRange = new DateRange(_createDateRange.Start, _createDateRange.End);
            }
            else
            {
                _createTempDateRange = null;
            }
            showCreatedPopover = !showCreatedPopover;
        }

        protected void TogglePublishedPopover()
        {
            if (_publishDateRange?.Start != null && _publishDateRange?.End != null)
            {
                _publishTempDateRange = new DateRange(_publishDateRange?.Start, _publishDateRange?.End);
            }
            else
            {
                 _publishTempDateRange = null;
            }
            showPublishedPopover = !showPublishedPopover;
        }

        protected void CancelCreatedPopover() => showCreatedPopover = false;
        protected void CancelPublishedPopover() => showPublishedPopover = false;

        protected async void ConfirmCreatedPopover()
        {
            _createDateRange = new DateRange(_createTempDateRange.Start, _createTempDateRange.End);
            dateCreatedFrom = _createTempDateRange.Start;
            dateCreatedTo = _createTempDateRange.End;
            showCreatedPopover = false;
            await OnDateFilterChanged.InvokeAsync((dateCreatedFrom, dateCreatedTo,datePublishedFrom,datePublishedTo));
            StateHasChanged();
        }

        protected async void ConfirmPublishedPopover()
        {
            _publishDateRange = new DateRange(_publishTempDateRange.Start, _publishTempDateRange.End);
            datePublishedFrom = _publishTempDateRange.Start;
            datePublishedTo = _publishTempDateRange.End;
            showPublishedPopover = false;
            await OnDateFilterChanged.InvokeAsync((dateCreatedFrom, dateCreatedTo,datePublishedFrom,datePublishedTo));
            StateHasChanged();
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

        protected async void ClearFilters()
        {
            _createDateRange = null;
             _publishDateRange = null;
            _publishTempDateRange = null;
            _createTempDateRange = null;
            dateCreatedFrom = null;
            dateCreatedTo = null;
            datePublishedFrom = null;
            datePublishedTo = null;
            ascending = true;
            searchText = string.Empty;
            await OnClearFilters.InvokeAsync();
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
                    ["Item Image"] = x.ImageUpload?.FirstOrDefault()?.Url,
                    ["Ad ID"] = x.Id,
                    ["User Id"] = x.UserId, 
                    ["Ad Title"] = x.AdTitle,
                    ["Username"] = x.UserName,
                    ["Category"] = x.Category,
                    ["Sub Category"] = x.SubCategory,
                    ["Certification"] = x.Certificate,           
                    ["Creation Date"] = x.CreationDate,
                    ["Published Date"] = x.DatePublished,
                    ["Date Expriry"] = x.DateExpiry,
                }).ToList();

            await JS.InvokeVoidAsync("exportToExcel", exportData, "Services_P2PListing.xlsx", "Transactions");

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

    }
}