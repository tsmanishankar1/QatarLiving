using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudExRichTextEditor;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using QLN.Common.Infrastructure.DTO_s;
using MudBlazor;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
namespace QLN.ContentBO.WebUI.Pages
{
    public class EventCreateFormBase : ComponentBase
    {
        [Inject]
        public IDialogService DialogService { get; set; }
        public EventDTO CurrentEvent { get; set; } = new EventDTO();
        [Inject] private IJSRuntime JS { get; set; }
        protected string? uploadedImage;
        protected MudExRichTextEdit Editor;
        protected string Category;
        protected DateRange SelectedDateRange = new DateRange(DateTime.Today, DateTime.Today.AddDays(1));
        protected string SelectedTimeOption = "General";
        protected string GeneralTime;

        public class DayTimeEntry
        {
            public DateTime Date { get; set; } 
            public string Day => Date.ToString("dddd");
            public bool IsSelected { get; set; }
            public string TimeRange { get; set; }
        }
        protected List<DayTimeEntry> DayTimeList = new();
        public double EventLat { get; set; } = 48.8584;
        public double EventLong { get; set; } = 2.2945;
        protected DateRange _dateRange
        {
            get => new(
                CurrentEvent.EventSchedule.StartDate.ToDateTime(TimeOnly.MinValue),
                CurrentEvent.EventSchedule.EndDate.ToDateTime(TimeOnly.MinValue)
            );
            set
            {
                if (value != null)
            {
                CurrentEvent.EventSchedule.StartDate = DateOnly.FromDateTime(value.Start ?? DateTime.Today);
                CurrentEvent.EventSchedule.EndDate = DateOnly.FromDateTime(value.End ?? DateTime.Today);
            }
    }
}
        protected List<string> Categories = new()
        {
            "Sports",
            "Music",
            "Education"
        };
        protected ElementReference _popoverDiv;
        
        protected bool _showDatePicker = false;
        protected string EventTitle;
        protected string AccessType = "Free Access";
        protected string TimeType = "General time";
        protected string LocationType = "Location";
        protected string Price;
        protected bool IsFeesSelected => AccessType == "Fees";
        protected string PriceFieldClass => IsFeesSelected ? "my-2 enable-field-style" : "my-2 price-class-style custom-border";
        protected string Location;
        protected string RedirectionLink;
        protected string Venue;
        protected DateTime? EventDate;
        protected TimeSpan? EventTime;
        protected string ArticleContent;
        protected string NewLocation;
        protected string SelectedDateLabel;
        protected DateRange _confirmedDateRange = new();
        [Parameter] public EventCallback<(string from, string to)> OnDateChanged { get; set; }
        public void Closed(MudChip<string> chip) => SelectedLocations.Remove(chip.Text);
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("initMap", 25.32, 51.54);
            }
        }
        [JSInvokable]
        public static Task UpdateLatLng(double lat, double lng)
        {
            Console.WriteLine($"New location: {lat}, {lng}");
            return Task.CompletedTask;
        }
        protected Task OpenDialogAsync()
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true
            };
            return DialogService.ShowAsync<MessageBox>(string.Empty, options);
        }
        protected void AddLocation()
        {
            if (!string.IsNullOrWhiteSpace(NewLocation) && !SelectedLocations.Contains(NewLocation))
            {
                SelectedLocations.Add(NewLocation.Trim());
                NewLocation = string.Empty;
            }
        }
        protected void HandleKeyPress(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                AddLocation();
            }
        }
        protected async Task UploadFiles(IBrowserFile file)
        {
            if (file is not null)
            {
                var buffer = new byte[file.Size];
                await file.OpenReadStream().ReadAsync(buffer);
                var base64 = Convert.ToBase64String(buffer);
                uploadedImage = $"data:{file.ContentType};base64,{base64}";
            }
        }
        protected void EditImage()
        {
            uploadedImage = null;
        }

        protected void DeleteImage()
        {
            uploadedImage = null;
        }
        protected Task EventAdded(string value)
        {
            return Task.CompletedTask;
        }
        protected List<string> SelectedLocations = new()
        {
            "Viva Bahriya - The Pearl Island"
        };

        protected void RemoveLocation()
        {
            CurrentEvent.Location = string.Empty;
        }
        protected async void CancelDatePicker()
        {
            _showDatePicker = false;
            EventDate = null;
            SelectedDateLabel = string.Empty;
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
                    TimeRange = ""
                });
            }
        }
    };
}
