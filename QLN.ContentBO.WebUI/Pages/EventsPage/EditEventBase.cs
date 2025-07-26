using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudExRichTextEditor;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using System.Text.Json;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.SuccessModal;
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
        [Inject] protected NavigationManager NavigationManager { get; set; }

        protected bool IsLoading = false;
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
        public string? _timeTypeError;
        public string? _eventTypeError;
        protected string? _timeError;
        protected string? _LocationError;
        protected string? _descriptionerror;
        protected string? _coverImageError;
        public string? _timeRangeDisplay;
        protected List<EventCategoryModel> Categories = [];
        protected void OnCancelClicked()
        {
            NavigationManager.NavigateTo("/manage/events");
        }

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
            public TimeSpan? StartTime { get; set; }
            public TimeSpan? EndTime { get; set; }
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
        protected string PriceFieldClass => IsFeesSelected ? "my-2 enable-field-style no-spinner" : "my-2 price-class-style custom-border no-spinner";
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
        protected bool IsPageLoading { get; set; } = true;

        protected MudFileUpload<IBrowserFile> _fileUpload;
        protected MudFileUpload<IBrowserFile> _fileUpload1;
        protected override async Task OnParametersSetAsync()
        {
            IsPageLoading = true;
            await Task.Delay(3000);
            try
            {
                Categories = await GetEventsCategories();
                var locationsResponse = await GetEventsLocations();
                Locations = locationsResponse ?? [];

                CurrentEvent = await GetEventById(Id);
                _editContext = new EditContext(CurrentEvent);

                SelectedLocationId = Locations?
                    .FirstOrDefault(loc => loc.Name.Equals(CurrentEvent?.Location, StringComparison.OrdinalIgnoreCase))
                        ?.Id;

                if (double.TryParse(CurrentEvent?.Latitude, out var lat) &&
                    double.TryParse(CurrentEvent?.Longitude, out var lng))
                {
                    latitude = lat;
                    Longitude = lng;
                }

                var startDate = CurrentEvent?.EventSchedule?.StartDate;
                var endDate = CurrentEvent?.EventSchedule?.EndDate;
                if (startDate.HasValue && endDate.HasValue &&
                    startDate.Value != DateOnly.MinValue && endDate.Value != DateOnly.MinValue)
                {
                    _dateRange = new DateRange(
                        startDate.Value.ToDateTime(TimeOnly.MinValue),
                        endDate.Value.ToDateTime(TimeOnly.MinValue)
                    );
                    SelectedDateLabel = $"{startDate.Value:dd-MM-yyyy} to {endDate.Value:dd-MM-yyyy}";
                }
                else
                {
                    _dateRange = null;
                    SelectedDateLabel = "No valid date selected";
                }

                var timeSlots = CurrentEvent?.EventSchedule?.TimeSlots ?? new List<TimeSlotModel>();
                if (CurrentEvent?.EventSchedule?.TimeSlotType == EventTimeType.PerDayTime)
                {
                    GeneratePerDayTimeList();
                }

                _shouldInitializeMap = true;
                IsPageLoading = false;
            }
            catch (Exception ex)
            {
                IsPageLoading = false;
                Logger.LogError(ex, "OnParametersSetAsync");
            }
        }


         private DotNetObjectReference<EditEventBase>? _dotNetRef;
       protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_shouldInitializeMap)
            {
                _shouldInitializeMap = false;

                if (_dotNetRef == null)
                {
                    _dotNetRef = DotNetObjectReference.Create(this);
                }

                await JS.InvokeVoidAsync("resetLeafletMap");
                await JS.InvokeVoidAsync("initializeMap", _dotNetRef);
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
            public Task SetCoordinates(double lat, double lng)
            {
                Logger.LogInformation("Map marker moved to Lat: {Lat}, Lng: {Lng}", lat, lng);

                // Update current event coordinates
                CurrentEvent.Latitude = lat.ToString();
                CurrentEvent.Longitude = lng.ToString();

                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => CurrentEvent.Latitude));
                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => CurrentEvent.Longitude));

                StateHasChanged(); // Reflect changes in UI
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
            if (_fileUpload is not null)
            {
                await _fileUpload.ResetAsync();
            }
            if (_fileUpload1 is not null)
            {
                await _fileUpload1.ResetAsync();
            }
        }
        protected void GeneratePerDayTimeList()
    {
        DayTimeList.Clear();
 
         if (_dateRange?.Start == null || _dateRange?.End == null)
            return;
 
        var start = _dateRange.Start.Value.Date;
        var end = _dateRange.End.Value.Date;
 
        var timeSlots = CurrentEvent?.EventSchedule?.TimeSlots ?? new List<TimeSlotModel>();
        for (var date = start; date <= end; date = date.AddDays(1))
            {
                var matchingSlot = timeSlots.FirstOrDefault(slot => slot.DayOfWeek == date.DayOfWeek);
 
                DayTimeList.Add(new DayTimeEntry
                {
                    Date = date,
                    IsSelected = matchingSlot != null,
                    StartTime = matchingSlot?.StartTime?.ToTimeSpan(),
                    EndTime = matchingSlot?.EndTime?.ToTimeSpan()
                });
            }
    }
        public void OpenTimeRangePicker()
        {
            _isTimeDialogOpen = true;
        }
         protected override async Task OnInitializedAsync()
        {
            try
            {
                await AuthorizedPage();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
                throw;
            }
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
                });
            }
        }
        protected bool IsValidTimeFormat(TimeSpan? start, TimeSpan? end)
        {
            if (!start.HasValue || !end.HasValue)
                return false;
            return end > start;
        }  
         private async Task ShowSuccessModal(string title)
        {
            var parameters = new DialogParameters
            {
                { nameof(SuccessModalBase.Title), title },
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true
            };

            await DialogService.ShowAsync<SuccessModal>("", parameters, options);
        }

        protected async Task HandleValidSubmit()
        {
            _DateError = null;
            _descriptionerror = null;
            _PriceError = null;
            _LocationError = null;
            _coverImageError = null;
            _timeError = null;
            bool hasError = false;
             if (CurrentEvent?.EventType == 0)
            {
                _eventTypeError = "Event Type is required.";
                Snackbar.Add("Event Type is required.", severity: Severity.Error);
                return;
            }
            if (CurrentEvent?.EventType == EventType.FeePrice && CurrentEvent.Price == null)
            {
                _PriceError = "Price is required for Fees events.";
                Snackbar.Add("Price is required for Fees events.", severity: Severity.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentEvent.Location))
            {
                _LocationError = "Location is required.";
                Snackbar.Add("Location is required.", severity: Severity.Error);
                return;
            }
              if (CurrentEvent?.EventSchedule?.TimeSlotType == 0)
            {
                _timeTypeError = "Time Type is required.";
                Snackbar.Add("Time Type is required.", severity: Severity.Error);
                return;
            }

            if (CurrentEvent?.EventSchedule == null || CurrentEvent.EventSchedule.StartDate == default)
            {
                _DateError = "Start date is required.";
                Snackbar.Add("Start date is required.", severity: Severity.Error);
                return;
            }
            else if (CurrentEvent.EventSchedule.TimeSlotType == EventTimeType.GeneralTime &&
             (CurrentEvent.EventSchedule.StartTime == null || CurrentEvent.EventSchedule.EndTime == null))
            {
                _timeError = "Start Time and End Time are required.";
                Snackbar.Add("Start Time and End Time are required.", severity: Severity.Error);
                return;
            }
            if (CurrentEvent.EventSchedule.TimeSlotType == EventTimeType.GeneralTime)
            {
                if (!IsValidTimeFormat(StartTimeSpan, EndTimeSpan))
                {
                    _timeError = "Please enter a valid start and end time.";
                    Snackbar.Add("Please enter a valid start and end time.", severity: Severity.Error);
                    return;
                }
            }
            if (CurrentEvent.EventSchedule.TimeSlotType == EventTimeType.FreeTextTime && CurrentEvent.EventSchedule.FreeTimeText == null)
            {
                _timeError = "Free text time is required";
                Snackbar.Add("Free text time is required", severity: Severity.Error);
                return;
            }


            if (string.IsNullOrWhiteSpace(CurrentEvent.EventDescription))
            {
                _descriptionerror = "Event description is required.";
                Snackbar.Add("Event description is required.", severity: Severity.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentEvent.CoverImage))
            {
                _coverImageError = "Cover Image is required.";
                Snackbar.Add("Cover Image is required.", severity: Severity.Error);
                return;
            }
            try
            {
                IsLoading = true;
                var response = await eventsService.UpdateEvents(CurrentEvent);
                if (response != null && response.IsSuccessStatusCode)
                {
                    await ShowSuccessModal("Events Updated Successfully");
                    await JS.InvokeVoidAsync("resetLeafletMap");
                    await JS.InvokeVoidAsync("initializeMap", _dotNetRef);
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
            finally
            {
                IsLoading = false;
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
        private async Task<List<LocationEventDto>> GetEventsLocations()
        {
            var flattenedList = new List<LocationEventDto>();
            try
            {
                var apiResponse = await eventsService.GetEventLocations();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<LocationListResponseDto>();
                    if (response?.Locations != null)
                    {
                        foreach (var location in response.Locations)
                        {
                            flattenedList.Add(location);
                            foreach (var area in location.Areas ?? Enumerable.Empty<AreaDto>())
                            {
                                var areaAsLocation = new LocationEventDto
                                {
                                    Id = area.Id,
                                    Name = $"{area.Name} , {location.Name}",
                                    Latitude = area.Latitude,
                                    Longitude = area.Longitude,
                                    Areas = new List<AreaDto>()
                                };
                                flattenedList.Add(areaAsLocation);
                            }
                        }
                    }
                }
                return flattenedList;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetFlattenedLocations");
                return new List<LocationEventDto>();
            }
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
                        // Update map marker position
                        await JS.InvokeVoidAsync("updateMapCoordinates", lat, lng);
                    }

                    StateHasChanged(); // Refresh UI
                }
            }
        protected void OnDaySelectionChanged(DayTimeEntry entry, object? value)
        {
            var existingSlot = CurrentEvent.EventSchedule.TimeSlots
                 .FirstOrDefault(ts => ts.DayOfWeek == entry.Date.DayOfWeek);
 
            if (existingSlot != null)
            {
                CurrentEvent.EventSchedule.TimeSlots.Remove(existingSlot);
            }
            else
            {
                CurrentEvent.EventSchedule.TimeSlots.Add(new TimeSlotModel
                {
                    DayOfWeek = entry.Date.DayOfWeek,
                    StartTime = entry.StartTime.HasValue ? TimeOnly.FromTimeSpan(entry.StartTime.Value) : null,
                    EndTime = entry.EndTime.HasValue ? TimeOnly.FromTimeSpan(entry.EndTime.Value) : null
                });
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
    _shouldInitializeMap = true;
    StateHasChanged();
}
        
    };
}