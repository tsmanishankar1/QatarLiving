using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.Subscription
{
    public class SubscriptionListingBase : QLComponentBase
    {
        [Inject] protected ILogger<SubscriptionListingBase> _logger { get; set; } = default!;

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
        protected int currentPage { get; set; } = 1;
        protected int pageSize { get; set; } = 12;

        protected readonly List<string> Categories = new()
        {
            "12 Months Basic",
    "12 Months Plus",
    "12 Months Super"
        };

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

        [Parameter] public EventCallback<(string from, string to)> OnDateChanged { get; set; }

        [Inject] protected IClassifiedService ClassifiedService { get; set; } = default!;
        protected List<SubscriptionListingModal> Listings { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadPrelovedListingsAsync();
        }

        private async Task LoadPrelovedListingsAsync()
        {
            try
            {
                IsLoading = true;

                var request = new FilterRequest
                {

                    PageNumber = currentPage,
                    PageSize = pageSize
                };

                var response = await ClassifiedService.GetPrelovedSubscription(request);

                if (response?.IsSuccessStatusCode == true)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<PrelovedSubscriptionResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Listings = [];

                    TotalCount = data?.TotalCount ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load preloved subscriptions: {ex.Message}");
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
            pageSize = newSize;
            currentPage = 1;
            await LoadPrelovedListingsAsync();
        }
        protected async Task OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString() ?? string.Empty;
            currentPage = 1;
            await LoadPrelovedListingsAsync();
        }


        private DateTime ParseDate(string date)
        {
            return DateTime.TryParse(date, out var result) ? result : DateTime.MinValue;
        }




        protected void ToggleSort()
        {
            // Example: toggle sort direction and update SortIcon
            SortIcon = SortIcon == Icons.Material.Filled.ArrowDownward
                ? Icons.Material.Filled.ArrowUpward
                : Icons.Material.Filled.ArrowDownward;

            // TODO: Perform actual sort operation
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
        protected List<string> SubscriptionTypes = new()
        {
            "Free",
            "Basic",
            "Pro",
            "Enterprise"
        };

        protected string SelectedSubscriptionType { get; set; } = null;

        protected Task OnSubscriptionChanged(string selected)
        {
            SelectedSubscriptionType = selected;
            return Task.CompletedTask;
        }
        // Date range logic
        protected DateRange _dateRange = new();
        protected DateRange _tempDateRange = new();



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


    }
}
