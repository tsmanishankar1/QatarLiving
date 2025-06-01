using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using Microsoft.JSInterop;
using MudBlazor;

namespace QLN.Web.Shared.Components.NewCustomSelect
{
    public class SearchSectionBase : ComponentBase, IDisposable

    {
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected ILogger<SearchSectionBase> Logger { get; set; }
[Inject] protected NavigationManager NavigationManager { get; set; }

        [Parameter] public List<EventCategory> Categories { get; set; } = [];

        protected List<SelectOption> PropertyTypes = new();
        protected string SelectedPropertyTypeId;

        protected bool _showDatePicker = false;
        protected string SelectedDateLabel = "";
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

        // Navigate to your desired URL with the selected date
        var url = $"https://www.qatarliving.com/events/day/{SelectedDateLabel}";
        NavigationManager.NavigateTo(url, forceLoad: true);

        StateHasChanged();
    }
}

protected void CancelDatePicker()
{
    _showDatePicker = false;
    _selectedDate = null;
    SelectedDateLabel = string.Empty;
    StateHasChanged();
}


        protected async Task OnRangeChanged(DateRange range)
        {
            _dateRange = range;
        }

      protected void HandleCategoryChanged(string id)
        {
            SelectedPropertyTypeId = id;

            if (!string.IsNullOrWhiteSpace(id))
            {
                // Navigate to new URL with type query string
                var targetUrl = $"https://www.qatarliving.com/events?type={id}";
                NavigationManager.NavigateTo(targetUrl, forceLoad: true); // forceLoad to trigger full page load
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

            return Task.CompletedTask;
        }

        public class SelectOption
        {
            public string Id { get; set; }
            public string Label { get; set; }
        }
    }
}
