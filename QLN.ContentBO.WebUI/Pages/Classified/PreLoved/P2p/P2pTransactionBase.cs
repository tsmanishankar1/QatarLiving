using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.P2p
{
    public class P2pTransactionBase : QLComponentBase
    {
        [Inject] protected IPrelovedService PrelovedService { get; set; } = default!;
        [Inject] protected ILogger<P2pTransactionBase> Logger { get; set; } = default!;
        [Parameter] public EventCallback<(string from, string to)> OnDateChanged { get; set; }
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        protected string SearchText { get; set; } = string.Empty;

        protected string SortIcon { get; set; } = Icons.Material.Filled.Sort;

        protected DateTime? dateCreated { get; set; }
        protected DateTime? datePublished { get; set; }

        protected DateTime? tempCreatedDate { get; set; }
        protected DateTime? tempPublishedDate { get; set; }

        protected bool showCreatedPopover { get; set; } = false;
        protected bool showPublishedPopover { get; set; } = false;

        protected bool _showDatePicker = false;
        public string? _timeTypeError;
        public string? _eventTypeError;
        protected List<LocationEventDto> Locations = new();
        public EventDTO CurrentEvent { get; set; } = new EventDTO();
        public string selectedLocation { get; set; } = string.Empty;
        public bool _isTimeDialogOpen = true;
        protected string? _DateError;
        protected string? _timeError;
        protected string? _PriceError;
        protected string? _LocationError;
        protected string? _descriptionerror;
        protected string? _coverImageError;
        public string? _timeRangeDisplay;
        protected EditContext _editContext;
        protected DateTime? EventDate;

        protected DateRange SelectedDateRange = new DateRange(DateTime.Today, DateTime.Today.AddDays(1));
        protected DateRange _confirmedDateRange = new();
        protected string SelectedDateLabel;
        protected string SelectedCategory { get; set; } = string.Empty;
        protected bool IsLoading { get; set; } = true;
        protected bool IsEmpty => !IsLoading && Listings.Count == 0;
        protected int TotalCount { get; set; }
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 12;

        protected readonly List<string> Categories =
        [
            "12 Months Basic",
            "12 Months Plus",
            "12 Months Super"
        ];

        public class DayTimeEntry
        {
            public DateTime Date { get; set; }
            public string Day => Date.ToString("dddd");
            public bool IsSelected { get; set; }
            public TimeSpan? StartTime { get; set; }
            public TimeSpan? EndTime { get; set; }
        }

        protected List<DayTimeEntry> DayTimeList = new();
        public double EventLat { get; set; } = 48.8584;
        public double EventLong { get; set; } = 2.2945;
        public bool _isDateRangeSelected = false;

        protected ElementReference _popoverDiv;

        protected List<PrelovedP2PTransactionItem> Listings { get; set; } = [];

        protected List<string> SubscriptionTypes = [];

        protected string SelectedSubscriptionType { get; set; } = null;
        // Date range logic
        protected DateRange _dateRange = new();
        protected DateRange _tempDateRange = new();
        protected bool showDatePopover = false;

        public bool IsSorted { get; set; } = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                    IsLoading = true;
                    await LoadPrelovedListingsAsync();
                    var tListOfSubsctiptions = await GetSubscriptionProductsAsync((int)VerticalTypeEnum.Classifieds, (int)SubVerticalTypeEnum.Preloved);
                    if (tListOfSubsctiptions != null && tListOfSubsctiptions.Count != 0)
                    {
                        SubscriptionTypes = [.. tListOfSubsctiptions.Select(x => x.ProductName)];
                    }
                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnAfterRenderAsync");
            }
        }

        private async Task LoadPrelovedListingsAsync()
        {
            try
            {
                IsLoading = true;

                var request = new PrelovedP2PTransactionQuery
                {
                    Page = CurrentPage,
                    PageSize = PageSize,
                    Search = SearchText,
                    CreatedDate = dateCreated?.ToString("yyyy-MM-dd"),
                    PublishedDate = datePublished?.ToString("yyyy-MM-dd"),
                    SortBy = "startDate",
                    SortOrder = IsSorted is true ? "asc" : "desc"
                };

                var response = await PrelovedService.GetPrelovedP2PTransaction(request) ?? new();

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<PrelovedP2PTransactionResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Listings = data?.Items ?? [];
                    TotalCount = data?.TotalCount ?? 0;
                }

            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load preloved subscriptions: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task HandlePageChanged(int newPage)
        {
            await LoadPrelovedListingsAsync();
        }

        protected async Task HandlePageSizeChanged(int newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadPrelovedListingsAsync();
        }

        protected async Task OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString() ?? string.Empty;
            CurrentPage = 1;
            await LoadPrelovedListingsAsync();
        }

        protected void ToggleSort()
        {
            // Example: toggle sort direction and update SortIcon
            SortIcon = SortIcon == Icons.Material.Filled.ArrowDownward
                ? Icons.Material.Filled.ArrowUpward
                : Icons.Material.Filled.ArrowDownward;

        }

        protected void ToggleCreatedPopover()
        {
            showCreatedPopover = !showCreatedPopover;
        }

        protected void CancelCreatedPopover()
        {
            tempCreatedDate = dateCreated;
            showCreatedPopover = false;
        }

        protected void ConfirmCreatedPopover()
        {
            dateCreated = tempCreatedDate;
            showCreatedPopover = false;
        }

        protected void TogglePublishedPopover()
        {
            showPublishedPopover = !showPublishedPopover;
        }

        protected void CancelPublishedPopover()
        {
            tempPublishedDate = datePublished;
            showPublishedPopover = false;
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
            SearchText = string.Empty;
            _dateRange = new();
            _tempDateRange = new();
        }
        protected async void CancelDatePicker()
        {
            _showDatePicker = false;
            EventDate = null;
            _confirmedDateRange = new();
            if (_dateRange?.Start != null || _dateRange?.End != null)
            {
                await OnDateChanged.InvokeAsync((null, null));
            }
            StateHasChanged();
        }
        protected async Task ApplyDatePicker()
        {
            if (_dateRange?.Start != null)
            {
                var startDate = _dateRange.Start.Value;
                var endDate = _dateRange.End ?? _dateRange.Start.Value;
                _confirmedDateRange = new DateRange(startDate, endDate);
                CurrentEvent.EventSchedule.StartDate = DateOnly.FromDateTime(startDate);
                CurrentEvent.EventSchedule.EndDate = DateOnly.FromDateTime(endDate);
                if (startDate.Date == endDate.Date)
                {
                    SelectedDateLabel = $"{startDate:dd-MM-yyyy}";
                    await OnDateChanged.InvokeAsync((startDate.ToString("yyyy-MM-dd"), null));
                }
                else
                {
                    SelectedDateLabel = $"{startDate:dd-MM-yyyy} to {endDate:dd-MM-yyyy}";
                    await OnDateChanged.InvokeAsync((startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd")));
                }
                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => CurrentEvent.EventSchedule.StartDate));
                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => CurrentEvent.EventSchedule.EndDate));
                _showDatePicker = false;
                GenerateDayTimeList();
                StateHasChanged();
            }
        }
        protected void ToggleDatePicker()
        {
            _showDatePicker = !_showDatePicker;

            if (_showDatePicker)
            {
                _dateRange = new DateRange(_confirmedDateRange.Start, _confirmedDateRange.End);
            }
        }
        protected void ClearSelectedDate()
        {
            if (!string.IsNullOrWhiteSpace(SelectedDateLabel))
            {
                SelectedDateLabel = null;
            }
            else
            {
                _showDatePicker = !_showDatePicker;

                if (_showDatePicker)
                {
                    _dateRange = new DateRange(_confirmedDateRange.Start, _confirmedDateRange.End);
                }
            }

        }
        protected void GenerateDayTimeList()
        {
            DayTimeList.Clear();

            if (_dateRange?.Start == null || _dateRange?.End == null)
                return;

            var start = _dateRange.Start.Value.Date;
            var end = _dateRange.End.Value.Date;

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                DayTimeList.Add(new DayTimeEntry
                {
                    Date = date,
                    IsSelected = false,
                });
            }
        }

        protected Task OnSubscriptionChanged(string selected)
        {
            SelectedSubscriptionType = selected;
            return Task.CompletedTask;
        }

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
        protected async Task ShowConfirmationExport()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Export Classified Items" },
                { "Descrption", "Do you want to export the current classified item data to Excel?" },
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
                if (Listings == null || !Listings.Any())
                {
                    Snackbar.Add("No data available to export.", Severity.Warning);
                    return;
                }

                var exportData = Listings.Select((x, index) => new Dictionary<string, object?>
                {
                    ["S.No."] = index + 1,
                    ["Ad ID"] = x?.AdId == null ? "-" : x.AdId,
                    ["Order Id"] = x?.OrderId == null ? "-" : x.OrderId,
                    ["Subscription Type"] = string.IsNullOrWhiteSpace(x.SubscriptionType) ? "-" : x.SubscriptionType,
                    ["User Name"] = string.IsNullOrWhiteSpace(x.UserName) ? "-" : x.UserName,
                    ["Email"] = string.IsNullOrWhiteSpace(x.Email) ? "-" : x.Email,
                    ["Mobile"] = string.IsNullOrWhiteSpace(x.Mobile) ? "-" : x.Mobile,
                    ["Whatsapp"] = string.IsNullOrWhiteSpace(x.Whatsapp) ? "-" : x.Whatsapp,
                    ["Amount"] = (x.Amount != 0) ? x.Amount.ToString("0.00") : "-",
                    ["Status"] = string.IsNullOrWhiteSpace(x.Status) ? "-" : x.Status,
                    ["Created Date"] = x.CreateDate.ToString("dd-MM-yyyy") ?? "-",
                    ["Published Date"] = (x.PublishedDate != default) ? x.PublishedDate.ToString("dd-MM-yyyy") : "-",
                    ["Start Date"] = (x.StartDate != default) ? x.StartDate.ToString("dd-MM-yyyy hh:mmtt") : "-",
                    ["End Date"] = (x.EndDate != default) ? x.EndDate.ToString("dd-MM-yyyy hh:mmtt") : "-",
                    ["Views"] = x.Views,
                    ["WhatsApp Click"] = x.WhatsAppLeads,
                    ["Phone Click"] = x.PhoneLeads
                }).ToList();

                await JS.InvokeVoidAsync("exportToExcel", exportData, "P2P_Listings.xlsx", "P2P Listings");

                Snackbar.Add("Export successful!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
            }
        }


    }
}
