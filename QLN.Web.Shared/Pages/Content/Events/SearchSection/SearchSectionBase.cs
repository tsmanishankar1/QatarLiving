using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components.LocationSelect;

namespace QLN.Web.Shared.Components.NewCustomSelect
{
    public class SearchSectionBase : ComponentBase, IDisposable

    {
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected ILogger<SearchSectionBase> Logger { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }

        [Parameter] public EventCallback<string> OnCategoryChanged { get; set; }
        [Parameter] public EventCallback<List<string>> OnLocationChanged { get; set; }

        [Parameter] public List<EventCategory> Categories { get; set; } = [];
        [Parameter] public EventCallback<(string from, string to)> OnDateChanged { get; set; }
        [Parameter] public List<Area> Areas { get; set; } = [];
        protected List<Area> FilteredAreas = new(); // filtered list for search
        protected List<Area> SelectedAreas = new(); // selected areas
        protected List<SelectOption> PropertyTypes = new();
        protected string SelectedPropertyTypeId;
        private string _fromDate;
        private string _toDate;
        protected LocationSelect<Area> _locationSelectRef;

        protected bool ShouldShowClearAll =>
            !string.IsNullOrEmpty(SelectedPropertyTypeId)
            || SelectedAreas.Any()
            || !string.IsNullOrEmpty(SelectedDateLabel);

        protected List<string> SelectedLocationIds { get; set; } = new();

        protected bool _showDatePicker = false;
        protected string SelectedDateLabel;
        protected bool _dateSelected = false;
        protected DateTime? _selectedDate;

        protected bool IsMobile = false;
        protected int windowWidth;
        private bool _jsInitialized = false;
        private bool _dateRangeApplied = false;
        private DateRange _confirmedDateRange = new();

        protected ElementReference _popoverDiv;

        protected DotNetObjectReference<SearchSectionBase>? _dotNetRef;

        private const int MobileBreakpoint = 770;

        protected MudDateRangePicker _pickerRef;
        protected DateRange _dateRange = new();


        protected void ToggleDatePicker()
        {
            _showDatePicker = !_showDatePicker;

            if (_showDatePicker)
            {
                // Set the picker to last confirmed value
                _dateRange = new DateRange(_confirmedDateRange.Start, _confirmedDateRange.End);
            }
        }


        protected MudDatePicker _datePickerRef;

        protected async Task ApplyDatePicker()
        {
            if (_dateRange?.Start != null)
            {
                var startDate = _dateRange.Start.Value;
                var endDate = _dateRange.End ?? _dateRange.Start.Value;

                _confirmedDateRange = new DateRange(startDate, endDate); // ← save confirmed range

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

                _showDatePicker = false;
                StateHasChanged();
            }
        }


        protected async Task PerformSearch(string keyword)
        {
            FilteredAreas = Areas
                .Where(a => a.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

            StateHasChanged();
        }

        protected async Task ClearAllFilters()
        {
            SelectedPropertyTypeId = null;
            SelectedLocationIds.Clear();
            SelectedAreas.Clear();
            FilteredAreas = Areas;
            _selectedDate = null;
            SelectedDateLabel = string.Empty;
            if (_locationSelectRef != null)
            {
                await _locationSelectRef.ClearSelection();
            }

            await OnCategoryChanged.InvokeAsync(null);
            await OnLocationChanged.InvokeAsync(null);
            await OnDateChanged.InvokeAsync((null, null));

            StateHasChanged();
        }

        protected async Task HandleLocationSelectionChanged(List<Area> selected)
        {
            SelectedAreas = selected;

            var selectedIds = SelectedAreas.Select(a => a.Id).ToList();

            if (selectedIds.Any())
            {
                await OnLocationChanged.InvokeAsync(selectedIds);
            }
            else
            {
                await OnLocationChanged.InvokeAsync(null);
            }
        }
        protected async void CancelDatePicker()
        {
            _showDatePicker = false;
            _selectedDate = null;
            SelectedDateLabel = string.Empty;
            _confirmedDateRange = new();
            if (_dateRange?.Start != null || _dateRange?.End != null)
            {
                await OnDateChanged.InvokeAsync((null, null));
            }

            StateHasChanged();
        }

        protected async Task HandleCategoryChanged(string id)
        {
            SelectedPropertyTypeId = id;

            await OnCategoryChanged.InvokeAsync(id);

        }

        protected async Task HandleLocationChanged(string location)
        {
            SelectedLocationIds = new List<string> { location };

            await OnLocationChanged.InvokeAsync(SelectedLocationIds);

            StateHasChanged();

        }

        protected async Task CallApiForLocationChange(List<Area> selected)
        {
            var selectedIds = selected.Select(a => a.Id).ToList();
            if (selectedIds.Any())
            {
                await OnLocationChanged.InvokeAsync(selectedIds);
            }
            else
            {
                await OnLocationChanged.InvokeAsync(null);
            }
        }
        protected void HandleDatePickerFocusOut(FocusEventArgs e)
        {
            _showDatePicker = false;
        }

        [JSInvokable]
        public void CloseDatePickerExternally()
        {
            _showDatePicker = false;
            StateHasChanged();
        }

        public void Dispose()
        {
            _dotNetRef?.Dispose();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                windowWidth = await JSRuntime.InvokeAsync<int>("getWindowWidth");
                IsMobile = windowWidth <= MobileBreakpoint;
                await JSRuntime.InvokeVoidAsync("registerResizeHandler", DotNetObjectReference.Create(this));
                _jsInitialized = true;
                StateHasChanged();
            }

            if (_showDatePicker)
            {
                _dotNetRef ??= DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("registerPopoverClickAway", _popoverDiv, _dotNetRef);
            }
        }



        [JSInvokable]
        public void UpdateWindowWidth(int width)
        {
            windowWidth = width;
            bool newIsMobile = windowWidth <= MobileBreakpoint;

            if (IsMobile != newIsMobile)
            {
                IsMobile = newIsMobile;
                StateHasChanged();
            }
        }


        protected override async Task OnInitializedAsync()
        {
            PropertyTypes = Categories.Select(cat => new SelectOption
            {
                Id = cat.Id,
                Label = cat.Name
            }).ToList();

            FilteredAreas = Areas;

            // 👇 Extract `perselect` query param from the URI
            var uri = new Uri(NavigationManager.Uri);
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (queryParams.TryGetValue("perselect", out var selectedId))
            {
                // Call the filter logic with the ID from query string
                await JSRuntime.InvokeVoidAsync("scrollToElementById", "search-section");
                await HandleCategoryChanged(selectedId);
            }

            return;
        }


        public class SelectOption
        {
            public string Id { get; set; }
            public string Label { get; set; }
        }
    }
}
