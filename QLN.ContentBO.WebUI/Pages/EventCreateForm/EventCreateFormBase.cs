using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudExRichTextEditor;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using System.Text.Json;
using MudBlazor;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using QLN.ContentBO.WebUI.Components;
using System.Net;
namespace QLN.ContentBO.WebUI.Pages
{
    public class EventCreateFormBase : QLComponentBase
    {
        [Inject] IEventsService eventsService { get; set; }
        [Inject]
        public IDialogService DialogService { get; set; }
        [Inject] ILogger<EventCreateFormBase> Logger { get; set; }
        protected EditContext _editContext;
        protected List<LocationEventDto> Locations = new();
        public EventDTO CurrentEvent { get; set; } = new EventDTO();
        public bool _isTimeDialogOpen = true;
        protected string? _DateError;
        protected string? _descriptionerror;
        public string? _timeRangeDisplay;
        protected List<EventCategoryModel> Categories = [];
        public List<string> MyItems = new()
    {
        "Option 1",
        "Option 2",
        "Option 3",
        "Option 4"
    };

        protected TimeSpan? StartTimeSpan
        {
            get => CurrentEvent.EventSchedule.StartTime.HasValue
                ? CurrentEvent.EventSchedule.StartTime.Value.ToTimeSpan()
                : (TimeSpan?)null;
            set
            {
                if (value.HasValue)
                {
                    CurrentEvent.EventSchedule.StartTime = TimeOnly.FromTimeSpan(value.Value);
                }
                else
                {
                    CurrentEvent.EventSchedule.StartTime = null;
                }
                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => CurrentEvent.EventSchedule.StartTime));
            }
        }


        protected TimeSpan? EndTimeSpan
        {
            get => CurrentEvent.EventSchedule.EndTime.HasValue
                ? CurrentEvent.EventSchedule.EndTime.Value.ToTimeSpan()
                : (TimeSpan?)null;
            set
            {
                if (value.HasValue)
                {
                    CurrentEvent.EventSchedule.EndTime = TimeOnly.FromTimeSpan(value.Value);
                }
                else
                {
                    CurrentEvent.EventSchedule.EndTime = null;
                }

                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => CurrentEvent.EventSchedule.EndTime));
            }

        }
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
        protected DateRange? _dateRange
        {
            get
            {
                if (CurrentEvent?.EventSchedule == null)
                    return null;

                return new DateRange(
                    CurrentEvent.EventSchedule.StartDate.ToDateTime(TimeOnly.MinValue),
                    CurrentEvent.EventSchedule.EndDate.ToDateTime(TimeOnly.MinValue)
                );
            }
            set
            {
                if (value != null && CurrentEvent?.EventSchedule != null)
                {
                    CurrentEvent.EventSchedule.StartDate = DateOnly.FromDateTime(value.Start ?? DateTime.Today);
                    CurrentEvent.EventSchedule.EndDate = DateOnly.FromDateTime(value.End ?? DateTime.Today);
                }
            }
        }
        protected ElementReference _popoverDiv;

        protected bool _showDatePicker = false;
        protected string EventTitle;
        protected string AccessType = "Free Access";
        protected string TimeType = "General time";
        protected string LocationType = "Location";
        protected string Price;
        protected bool IsFeesSelected => CurrentEvent.EventType == EventType.FeePrice;
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
        protected string SelectedLocationId;
        protected override async Task OnInitializedAsync()
        {
            CurrentEvent ??= new EventDTO();
            CurrentEvent.EventSchedule ??= new EventScheduleModel();
            _editContext = new EditContext(CurrentEvent);
            Categories = await GetEventsCategories();
            Console.WriteLine("Categories:");
            Console.WriteLine(JsonSerializer.Serialize(Categories, new JsonSerializerOptions { WriteIndented = true }));
            var locationsResponse = await GetEventsLocations();
            Locations = locationsResponse?.Locations ?? [];
        }
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
        public void UpdateTimeSlotListFromDayTimeList()
        {
            CurrentEvent.EventSchedule.TimeSlots = DayTimeList
                .Where(entry => entry.IsSelected && !string.IsNullOrWhiteSpace(entry.TimeRange))
                .Select(entry => new TimeSlotModel
                {
                    DayOfWeek = entry.Date.DayOfWeek,
                    Time = entry.TimeRange
                })
             .ToList();
        }
        protected void OnTimeChanged(DayTimeEntry entry, string? newTime)
        {
            entry.TimeRange = newTime ?? string.Empty;
            UpdateTimeSlotListFromDayTimeList();
        }
        protected void OnDaySelectionChanged(DayTimeEntry entry, object? value)
        {
            Console.Write("check box is selected");
            entry.IsSelected = (bool)value!;
            UpdateTimeSlotListFromDayTimeList();
        }
        public void OpenTimeRangePicker()
        {
            _isTimeDialogOpen = true;
        }
        protected void ApplyTimeRange()
        {
            if (CurrentEvent.EventSchedule.StartTime.HasValue && CurrentEvent.EventSchedule.EndTime.HasValue)
            {
                _timeRangeDisplay = $"{DateTime.Today.Add(CurrentEvent.EventSchedule.StartTime.Value.ToTimeSpan()):h:mm tt} to {DateTime.Today.Add(CurrentEvent.EventSchedule.EndTime.Value.ToTimeSpan()):h:mm tt}";
            }
            else
            {
                _timeRangeDisplay = string.Empty;
            }
            _isTimeDialogOpen = false;
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
        protected async Task HandleValidSubmit()
        {
            _DateError = null;
            if (CurrentEvent.EventSchedule.StartDate == default)
            {
                _DateError = "Start date is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(CurrentEvent.EventDescription))
            {
                _descriptionerror = "Event description is required.";
                StateHasChanged();
                return;
            }
            if (CurrentEvent.EventSchedule == null || CurrentEvent.EventSchedule.StartDate == default)
            {
                _DateError = "Start date is required.";
                return;
            }

            _descriptionerror = null;
            Console.Write("the method is called !!!");
            try
            {
                Console.Write("the api response is");
                Console.WriteLine("CurrentEvent:");
                Console.WriteLine(JsonSerializer.Serialize(CurrentEvent, new JsonSerializerOptions { WriteIndented = true }));
                var response = await eventsService.CreateEvent(CurrentEvent);
                if (response != null && response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Events Added", severity: Severity.Success);

                    var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
                    // await DialogService.ShowAsync<ArticlePublishedDialog>("", options);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action");
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error");
                }
                // CurrentEvent = new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetWriterTags");
            }
        }
        private async Task<List<EventCategoryModel>> GetEventsCategories()
        {
            try
            {
                var apiResponse = await eventsService.GetEventCategories();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var rawContent = await apiResponse.Content.ReadAsStringAsync();
                    Console.WriteLine(rawContent); // Log the raw JSON

                    // Then deserialize
                    var result = JsonSerializer.Deserialize<List<EventCategoryModel>>(
                                rawContent,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result ?? [];
                }
                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsCategories");
                return [];
            }
        }
        private async Task<LocationListResponseDto> GetEventsLocations()
        {
            try
            {
                var apiResponse = await eventsService.GetEventLocations();
                if (apiResponse.IsSuccessStatusCode)
                {
                    Console.Write("the api response is");
                    var response = await apiResponse.Content.ReadFromJsonAsync<LocationListResponseDto>();
                    return response ?? new LocationListResponseDto();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsLocations");
            }
            return new LocationListResponseDto();
        }
        protected async Task OnLocationChanged(string locationId)
{
            Console.Write("this method is called");
    SelectedLocationId = locationId;

    var selectedLocation = Locations.FirstOrDefault(l => l.Id == locationId);
            if (selectedLocation != null)
            {
                CurrentEvent.Location = selectedLocation.Name;
                Console.WriteLine($"Selected location: {selectedLocation.Name}");

                if (double.TryParse(selectedLocation.Latitude, out var lat) &&
                    double.TryParse(selectedLocation.Longitude, out var lng))
                {
                    await JS.InvokeVoidAsync("initMap", lat, lng);
                }
                StateHasChanged();
    }
}
    };
}

