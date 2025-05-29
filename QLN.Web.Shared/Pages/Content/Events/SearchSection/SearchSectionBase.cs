using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor; 

namespace QLN.Web.Shared.Components.NewCustomSelect
{
    public class SearchSectionBase : ComponentBase
    {
        [Inject] protected IJSRuntime JSRuntime { get; set; }

        protected List<SelectOption> PropertyTypes = new();
        protected string SelectedPropertyTypeId;

        protected bool _showDatePicker = false;
        protected string SelectedDateLabel = "";
        protected bool _dateSelected = false;
        protected DateTime? _selectedDate;

        protected bool IsMobile = false;
        protected int windowWidth;
        private bool _jsInitialized = false;

        private const int MobileBreakpoint = 770;

        protected MudDateRangePicker _pickerRef;
        protected DateRange _dateRange = new ();



        protected void ToggleDatePicker() => _showDatePicker = !_showDatePicker;
       protected void CancelDatePicker()
        {
            _showDatePicker = false;
            _dateRange = new DateRange(null, null); // Clear selected range
            SelectedDateLabel = string.Empty;       // Clear the display label
            StateHasChanged();
        }

protected async Task OnRangeChanged(DateRange range)
{
    _dateRange = range;
}

protected async Task ApplyDatePicker()
{
    if (_dateRange?.Start != null && _dateRange?.End != null)
    {
        var start = _dateRange.Start.Value.ToString("dd-MM-yyyy");
        var end = _dateRange.End.Value.ToString("dd-MM-yyyy");

        SelectedDateLabel = (_dateRange.Start.Value.Date == _dateRange.End.Value.Date)
            ? start
            : $"{start} to {end}";

        _showDatePicker = false; // Close picker after applying dates
        StateHasChanged();
    }
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
            PropertyTypes = new List<SelectOption>
            {
                new SelectOption { Id = "1", Label = "Apartment" },
                new SelectOption { Id = "2", Label = "Villa" },
                new SelectOption { Id = "3", Label = "Hotel Stay" },
                new SelectOption { Id = "4", Label = "Shared" }
                
            };

            return Task.CompletedTask;
        }

        public class SelectOption
        {
            public string Id { get; set; }
            public string Label { get; set; }
        }
    }
}
