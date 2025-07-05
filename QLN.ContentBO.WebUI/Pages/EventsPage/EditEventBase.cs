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
using Markdig.Syntax;
namespace QLN.ContentBO.WebUI.Pages
{
    public class EditEventBase : QLComponentBase
    {
        [Inject] IEventsService eventsService { get; set; }
        [Inject]
        public IDialogService DialogService { get; set; }
        [Parameter]
        public Guid Id { get; set; }
        [Inject] ILogger<EventCreateFormBase> Logger { get; set; }
        protected EditContext _editContext;
        private bool _shouldInitializeMap = false;
        protected List<LocationEventDto> Locations = new();
        public EventDTO CurrentEvent { get; set; } = new EventDTO();
        public bool _isTimeDialogOpen = true;
        protected string? _DateError;
        protected string? _PriceError;
        protected string? _LocationError;
        protected string? _descriptionerror;
        protected string? _coverImageError;
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
        protected Double latitude = 25.32;
        protected Double Longitude = 51.54;
        protected string SelectedDateLabel;
        protected DateRange _confirmedDateRange = new();
        [Parameter] public EventCallback<(string from, string to)> OnDateChanged { get; set; }
        public void Closed(MudChip<string> chip) => SelectedLocations.Remove(chip.Text);
        protected string SelectedLocationId;
        protected override async Task OnParametersSetAsync()
        {
            Categories = await GetEventsCategories();
            var locationsResponse = await GetEventsLocations();
            Locations = locationsResponse?.Locations ?? [];
            CurrentEvent.EventSchedule = new EventScheduleModel();
            CurrentEvent = await GetEventById(Id);
            _editContext = new EditContext(CurrentEvent);
            SelectedLocationId = Locations
            ?.FirstOrDefault(loc => loc.Name.Equals(CurrentEvent?.Location, StringComparison.OrdinalIgnoreCase))
                 ?.Id;
            if (double.TryParse(CurrentEvent.Latitude, out var lat) &&
               double.TryParse(CurrentEvent.Longitude, out var lng))
            {
                latitude = lat;
                Longitude = lng;
            }
            _dateRange = new DateRange(CurrentEvent?.EventSchedule?.StartDate.ToDateTime(TimeOnly.MinValue), CurrentEvent?.EventSchedule?.EndDate.ToDateTime(TimeOnly.MinValue));
            var startDate = CurrentEvent?.EventSchedule?.StartDate;
            var endDate = CurrentEvent?.EventSchedule?.EndDate;
            // Console.Write("the start date is " + CurrentEvent?.EventSchedule?.StartDate);
            // Console.Write("the start date is " + CurrentEvent?.EventSchedule?.EndDate);
            if (startDate.HasValue && startDate.Value != DateOnly.MinValue &&
                endDate.HasValue && endDate.Value != DateOnly.MinValue)
            {
                SelectedDateLabel = $"{startDate.Value:dd-MM-yyyy} to {endDate.Value:dd-MM-yyyy}";
            }
            else
            {
                SelectedDateLabel = "No valid date selected";
            }
            Console.WriteLine($"Start: {startDate}, End: {endDate}");
            _shouldInitializeMap = true;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_shouldInitializeMap)
            {
                _shouldInitializeMap = false;
                await JS.InvokeVoidAsync("initMap", latitude, Longitude);
            }
        }
        public static DateRange? ConvertToDateRange(DateOnly? startDate, DateOnly? endDate)
        {
            if (startDate == null || endDate == null)
                return null;

            return new DateRange(
                startDate.Value.ToDateTime(TimeOnly.MinValue),
                endDate.Value.ToDateTime(TimeOnly.MinValue)
    );
}
        [JSInvokable]
        public static Task UpdateLatLng(double lat, double lng)
        {
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
        protected async Task HandleFilesChanged(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file != null)
            {
                using var stream = file.OpenReadStream(5 * 1024 * 1024);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                CurrentEvent.CoverImage = $"data:{file.ContentType};base64,{base64}";
                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => CurrentEvent.CoverImage));
                _coverImageError = null;
            }
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
            CurrentEvent.CoverImage = null;
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
                 if (CurrentEvent?.EventSchedule == null)
                {
                    CurrentEvent.EventSchedule = new EventScheduleModel();
                }
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
            _descriptionerror = null;
            _PriceError = null;
            _LocationError = null;
            if (CurrentEvent.EventSchedule.StartDate == default)
            {
                _DateError = "Start date is required.";
                StateHasChanged();
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
                StateHasChanged();
                return;
            }
            if (CurrentEvent.EventType == EventType.FeePrice && CurrentEvent.Price == null)
            {
                _PriceError = "Price is required for Fees events.";
                StateHasChanged();
                return;
            }
            if (string.IsNullOrWhiteSpace(CurrentEvent.Location))
            {
                _LocationError = "Location is required.";
                StateHasChanged();
                return;
            }
            if (string.IsNullOrWhiteSpace(CurrentEvent.CoverImage))
            {
                _coverImageError = "Cover Image is required.";
                StateHasChanged();
                return;
            }
            try
            {
                var response = await eventsService.UpdateEvents(CurrentEvent);
                if (response != null && response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Events Updated Successfully", severity: Severity.Success);
                    var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action");
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error");
                }
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
        private async Task<EventDTO> GetEventById(Guid Id)
        {
            try
            {
                var apiResponse = await eventsService.GetEventById(Id);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<EventDTO>();
                    return response ?? new EventDTO();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsLocations");
            }
            return new EventDTO();
        }
        protected async Task OnLocationChanged(string locationId)
        {
            SelectedLocationId = locationId;

            var selectedLocation = Locations.FirstOrDefault(l => l.Id == locationId);
            if (selectedLocation != null)
            {
                CurrentEvent.Location = selectedLocation.Name;
                CurrentEvent.Latitude = selectedLocation.Latitude;
                CurrentEvent.Longitude = selectedLocation.Longitude;
                if (double.TryParse(selectedLocation.Latitude, out var lat) &&
                    double.TryParse(selectedLocation.Longitude, out var lng))
                {
                    await JS.InvokeVoidAsync("initMap", lat, lng);
                }
                StateHasChanged();
            }
        }
        protected void OnDaySelectionChanged(DayTimeEntry entry, object? value)
        {
            entry.IsSelected = (bool)value!;

            if (entry.IsSelected && !string.IsNullOrWhiteSpace(entry.TimeRange))
            {
                var existing = CurrentEvent.EventSchedule.TimeSlots
                    .FirstOrDefault(t => t.DayOfWeek == entry.Date.DayOfWeek);

                if (existing != null)
                {
                    existing.Time = entry.TimeRange;
                }
                else
                {
                    CurrentEvent.EventSchedule.TimeSlots.Add(new TimeSlotModel
                    {
                        DayOfWeek = entry.Date.DayOfWeek,
                        Time = entry.TimeRange
                    });
                }
            }
            else
            {
                CurrentEvent.EventSchedule.TimeSlots.RemoveAll(t => t.DayOfWeek == entry.Date.DayOfWeek);
            }
        }
        private void ClearForm()
        {
            CurrentEvent = new EventDTO
            {
                EventSchedule = new EventScheduleModel(),
                FeaturedSlot = new Slot(),
                IsActive = true,
                IsFeatured = false
            };

            SelectedDateLabel = string.Empty;
            StartTimeSpan = null;
            EndTimeSpan = null;
            _dateRange = null;
            DayTimeList.Clear();
            _descriptionerror = string.Empty;
            _PriceError = string.Empty;
    _coverImageError = string.Empty;
    uploadedImage = null;
    SelectedLocationId = null;
    StateHasChanged();
}
        protected void OnTimeChanged(DayTimeEntry entry, string? newTime)
        {
            entry.TimeRange = newTime ?? string.Empty;
            if (entry.IsSelected && !string.IsNullOrWhiteSpace(entry.TimeRange))
            {
                var existing = CurrentEvent.EventSchedule.TimeSlots
                    .FirstOrDefault(t => t.DayOfWeek == entry.Date.DayOfWeek);

                if (existing != null)
                {
                    existing.Time = entry.TimeRange;
                }
                else
                {
                    CurrentEvent.EventSchedule.TimeSlots.Add(new TimeSlotModel
                    {
                        DayOfWeek = entry.Date.DayOfWeek,
                        Time = entry.TimeRange
                    });
                }
            }
        }
    };
}

