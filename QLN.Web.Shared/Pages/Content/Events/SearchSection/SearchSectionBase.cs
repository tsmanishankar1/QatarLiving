using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Pages.Content.News;

namespace QLN.Web.Shared.Components.NewCustomSelect
{
    public class SearchSectionBase : ComponentBase, IDisposable

    {
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected ILogger<SearchSectionBase> Logger { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }

        [Parameter] public EventCallback<string> OnCategoryChanged { get; set; }
        [Parameter] public EventCallback<string> OnLocationChanged { get; set; }
        [Parameter] public EventCallback<string> OnDateChanged { get; set; }

        [Parameter] public List<EventCategory> Categories { get; set; } = [];

        [Parameter] public List<Area> Areas { get; set; } = [];
        protected List<Area> FilteredAreas = new(); // filtered list for search
        protected List<Area> SelectedAreas = new(); // selected areas
        protected List<SelectOption> PropertyTypes = new();
        protected string SelectedPropertyTypeId;
        protected bool ShouldShowClearAll =>
            !string.IsNullOrEmpty(SelectedPropertyTypeId)
            || SelectedAreas.Any()
            || !string.IsNullOrEmpty(SelectedDateLabel);

        protected string SelectedLocationId;

        protected bool _showDatePicker = false;
        protected string SelectedDateLabel;
        protected bool _dateSelected = false;
        protected DateTime? _selectedDate;

        protected bool IsMobile = false;
        protected int windowWidth;
        private bool _jsInitialized = false;
        protected ElementReference _popoverDiv;

        protected DotNetObjectReference<SearchSectionBase>? _dotNetRef;

        private const int MobileBreakpoint = 770;

        protected MudDateRangePicker _pickerRef;
        protected DateRange _dateRange = new();


        protected void ToggleDatePicker() => _showDatePicker = !_showDatePicker;
        protected MudDatePicker _datePickerRef;

        protected async Task ApplyDatePicker()
        {
            if (_selectedDate != null)
            {
                SelectedDateLabel = _selectedDate.Value.ToString("yyyy-MM-dd"); // format for URL
                _showDatePicker = false;
                await OnDateChanged.InvokeAsync(SelectedDateLabel);
                 StateHasChanged(); 
            }
        }
        protected async Task PerformSearch(string keyword)
        {
            // Filter areas based on name
            FilteredAreas = Areas
                .Where(a => a.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Assign filtered list back to trigger update
            StateHasChanged();
        }

        protected async Task ClearAllFilters()
        {
            SelectedPropertyTypeId = null;
            SelectedLocationId = null;
            SelectedAreas.Clear();
            FilteredAreas = Areas; // Reset area filter
            _selectedDate = null;
            SelectedDateLabel = string.Empty;

            await OnCategoryChanged.InvokeAsync(null);
            await OnLocationChanged.InvokeAsync(null);
            await OnDateChanged.InvokeAsync(null);

            StateHasChanged(); // Reflect UI changes
        }

        protected async Task HandleLocationSelectionChanged(List<Area> selected)
        {
            SelectedAreas = selected;

            // Assuming only one is selected at a time for search
            var selectedId = SelectedAreas.FirstOrDefault()?.Id;

            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                await OnLocationChanged.InvokeAsync(selectedId);
            }
        }
       protected async void CancelDatePicker()
        {
            _showDatePicker = false;
            _selectedDate = null;
            SelectedDateLabel = string.Empty;

            // ✅ Trigger API call
            await OnDateChanged.InvokeAsync(null);

            StateHasChanged(); // Optional, to update UI
        }


        protected async Task OnRangeChanged(DateRange range)
        {
            _dateRange = range;
        }

        protected async Task HandleCategoryChanged(string id)
        {
            SelectedPropertyTypeId = id;

            await OnCategoryChanged.InvokeAsync(id);

        }

        protected async Task HandleLocationChanged(string location)
        {
            SelectedLocationId = location;

            await OnLocationChanged.InvokeAsync(location);

            StateHasChanged();

        }

        protected async Task CallApiForLocationChange(List<Area> selected)
        {
            var selectedId = selected.FirstOrDefault()?.Id;
            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                await OnLocationChanged.InvokeAsync(selectedId);
            }
            else
            {
                // Handle location removed case (e.g., call API with null or empty)
                await OnLocationChanged.InvokeAsync(null);
                // await MyApiService.ClearLocationAsync();
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
                _dotNetRef ??= DotNetObjectReference.Create(this); // ✅ Fix type here too
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


        protected override Task OnInitializedAsync()
        {
            PropertyTypes = Categories.Select(cat => new SelectOption
            {
                Id = cat.Id,
                Label = cat.Name
            }).ToList();
            FilteredAreas = Areas;
            return Task.CompletedTask;
        }

        public class SelectOption
        {
            public string Id { get; set; }
            public string Label { get; set; }
        }
    }
}
