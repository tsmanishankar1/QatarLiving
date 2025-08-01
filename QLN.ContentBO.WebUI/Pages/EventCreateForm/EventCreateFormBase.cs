using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudExRichTextEditor;
using PSC.Blazor.Components.MarkdownEditor;
using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.News;
using QLN.ContentBO.WebUI.Components.SuccessModal;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using QLN.ContentBO.WebUI.Pages.EventsPage;
using System.Net;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages
{
    public class EventCreateFormBase : QLComponentBase
    {
        [Inject] IEventsService eventsService { get; set; }
        [Inject]
        public IDialogService DialogService { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        protected bool IsLoading = false;
        [Inject] ILogger<EventCreateFormBase> Logger { get; set; }
        protected EditContext _editContext;
        public string? _timeTypeError;
        public string? _eventTypeError;
        protected List<LocationEventDto> Locations = new();
        public EventDTO CurrentEvent { get; set; } = new EventDTO();
        public string selectedLocation { get; set; } = string.Empty;
        public bool _isTimeDialogOpen = true;
        protected string? _DateError;
        protected string? _timeError;
        protected string? _PriceError;
        protected string? _CategoryError;
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

        protected string? GeneralFreeTextTime { get; set; }

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
            public string? FreeTextTime { get; set; }
            public string? ErrorMsg { get; set; }
        }

        protected List<DayTimeEntry> DayTimeList = new();

        public double EventLat { get; set; } = 48.8584;

        public double EventLong { get; set; } = 2.2945;

        public bool _isDateRangeSelected = false;

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
                    var start = value.Start ?? DateTime.Today;
                    var end = value.End ?? start;
                    CurrentEvent.EventSchedule.StartDate = DateOnly.FromDateTime(start);
                    CurrentEvent.EventSchedule.EndDate = DateOnly.FromDateTime(end);
                    _isDateRangeSelected = true;
                }
                StateHasChanged();
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
        protected string SelectedDateLabel;
        protected DateRange _confirmedDateRange = new();
        [Parameter] public EventCallback<(string from, string to)> OnDateChanged { get; set; }
        public void Closed(MudChip<string> chip) => SelectedLocations.Remove(chip.Text);
        protected string SelectedLocationId;
        private bool _shouldInitializeMap = true;

        protected MudFileUpload<IBrowserFile> _fileUpload;

        // Custom Markdown Editor Properties
        protected MarkdownEditor MarkdownEditorRef;
        protected MudFileUpload<IBrowserFile> _markdownfileUploadRef;
        protected string UploadImageButtonName { get; set; } = "uploadImage";
        protected string BlobContainerName { get; set; } = "content-images";

        protected override async Task OnInitializedAsync()
        {
            await AuthorizedPage();
            CurrentEvent ??= new EventDTO();
            CurrentEvent.EventSchedule ??= new EventScheduleModel();
            CurrentEvent.EventSchedule.TimeSlotType = EventTimeType.GeneralTime;
            _editContext = new EditContext(CurrentEvent);
            Categories = await GetEventsCategories();
            var locationsResponse = await GetEventsLocations();
            Locations = locationsResponse ?? [];
            CurrentEvent.EventDescription = string.Empty;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                    _dotNetRef = DotNetObjectReference.Create(this);

                    await JS.InvokeVoidAsync("resetLeafletMap");
                    await JS.InvokeVoidAsync("initializeMap", _dotNetRef);
                    await MarkdownEditorRef.SetValueAsync(CurrentEvent.EventDescription);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnAfterRenderAsync");
            }
        }

        protected async void OnCancelClicked()
        {
            var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await DialogService.ShowAsync<DiscardArticleDialog>("", options);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                ClearForm();
                await JS.InvokeVoidAsync("resetLeafletMap");
                await JS.InvokeVoidAsync("initializeMap", _dotNetRef);
                StateHasChanged();
            }
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

        protected async Task DeleteEventOnClick()
        {
            var parameters = new DialogParameters
         {
            { "OnAdd", EventCallback.Factory.Create(this, ClearForm) }
        };
            var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = DialogService.Show<EventDiscardArticle>("", parameters, options);
            var result = await dialog.Result;
        }

        protected async Task HandleFilesChanged(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    using var stream = file.OpenReadStream(2 * 1024 * 1024);
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var base64 = Convert.ToBase64String(memoryStream.ToArray());
                    CurrentEvent.CoverImage = $"data:{file.ContentType};base64,{base64}";
                    _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => CurrentEvent.CoverImage));
                    _coverImageError = null;
                    _fileUpload?.ResetValidation();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleFilesChanged");
            }
        }

        public void OpenTimeRangePicker()
        {
            _isTimeDialogOpen = true;
        }

        protected void OnTimeSlotTypeChanged(EventTimeType newValue)
        {
            CurrentEvent.EventSchedule.TimeSlotType = newValue;

            if (CurrentEvent.EventSchedule.TimeSlotType == EventTimeType.GeneralTime)
            {
                DayTimeList = [];
                CurrentEvent.EventSchedule.TimeSlots = [];
            }
            else if (CurrentEvent.EventSchedule.TimeSlotType == EventTimeType.PerDayTime)
            {
                GeneralFreeTextTime = null;
                CurrentEvent.EventSchedule.GeneralTextTime = null;
                GenerateDayTimeList();
            }
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

        protected async void EditImage()
        {
            await _fileUpload.OpenFilePickerAsync();
        }

        protected void RemoveImage()
        {
            CurrentEvent.CoverImage = null;
            _fileUpload?.ResetValidation();
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
        protected async Task HandleValidSubmit()
        {
            _DateError = null;
            _descriptionerror = null;
            _CategoryError = null;
            _PriceError = null;
            _LocationError = null;
            _timeError = null;
            _coverImageError = null;
            bool hasError = false;
            if (string.IsNullOrWhiteSpace(SelectedDateLabel))
            {
                Snackbar.Add("Date is required", severity: Severity.Error);
                return;
            }
            if (CurrentEvent?.CategoryId == 0)
            {
                _CategoryError = "Category is required.";
                Snackbar.Add("Category is required.", severity: Severity.Error);
                return;
            }
            if (CurrentEvent?.EventType == 0)
            {
                _eventTypeError = "Event Type is required.";
                Snackbar.Add("Event Type is required.", severity: Severity.Error);
                return;
            }
            if (CurrentEvent?.EventType == EventType.FeePrice && CurrentEvent.Price == null)
            {
                Snackbar.Add("Price is required for Fees events.", severity: Severity.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentEvent.Location))
            {
                _LocationError = "Location is required.";
                Snackbar.Add("Location is required.", severity: Severity.Error);
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

            if (CurrentEvent.EventSchedule == null || CurrentEvent.EventSchedule.StartDate == default)
            {
                _DateError = "Start date is required.";
                Snackbar.Add("Start date is required.", severity: Severity.Error);
                return;
            }
            else if (CurrentEvent.EventSchedule.TimeSlotType == EventTimeType.PerDayTime)
            {
                if (!DayTimeList.Any(slot => slot.IsSelected))
                {
                    Snackbar.Add("Time are required.", severity: Severity.Error);
                    return;
                }
                foreach (var slot in DayTimeList)
                {
                    if (string.IsNullOrEmpty(slot.FreeTextTime) && slot.IsSelected)
                    {
                        slot.ErrorMsg = "Time are required.";
                        hasError = true;
                    }
                    else
                    {
                        slot.ErrorMsg = null;
                    }
                }
                if (hasError)
                {
                    Snackbar.Add("Time are required.", severity: Severity.Error);
                    return;
                }
            }
            if (CurrentEvent.EventSchedule.TimeSlotType == EventTimeType.GeneralTime)
            {
                if (!IsValidTimeFormat(GeneralFreeTextTime))
                {
                    _timeError = "Time are required.";
                    Snackbar.Add("Time are required.", severity: Severity.Error);
                    return;
                }
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
            await ShowConfirmation(
            "Save Event",
            "Are you sure you want to save this event?",
            "Save", async () => await SaveEvent());
        }
        protected async Task SaveEvent()
        {
            try
            {
                IsLoading = true;
                CurrentEvent.EventSchedule.TimeSlots ??= [];
                if (CurrentEvent.EventSchedule.TimeSlotType == EventTimeType.GeneralTime)
                {
                    CurrentEvent.EventSchedule.GeneralTextTime = GeneralFreeTextTime;
                }
                foreach (var entry in DayTimeList)
                {
                    if (entry.IsSelected)
                    {
                        CurrentEvent.EventSchedule.TimeSlots.Add(new TimeSlotModel
                        {
                            DayOfWeek = entry.Date.DayOfWeek,
                            TextTime = entry.FreeTextTime,
                        });
                    }
                }
                if (int.TryParse(SelectedLocationId, out int value))
                {
                    CurrentEvent.LocationId = value;
                }
                CurrentEvent.Status = EventStatus.Published;
                var response = await eventsService.CreateEvent(CurrentEvent);
                if (response != null && response.IsSuccessStatusCode)
                {
                    await ShowSuccessModal("Events Added successfully!");
                    ClearForm();
                    await JS.InvokeVoidAsync("resetLeafletMap");
                    await JS.InvokeVoidAsync("initializeMap", _dotNetRef);
                    var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };


                }
                else if (response?.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Snackbar.Add($"Bad Request: {errorContent}", Severity.Error);
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

        private DotNetObjectReference<EventCreateFormBase>? _dotNetRef;

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

        protected async Task OnLocationChanged(string locationId)
        {
            SelectedLocationId = locationId;

            var selectedLocation = Locations.FirstOrDefault(l => l.Id == locationId);
            if (selectedLocation != null)
            {
                CurrentEvent.Location = selectedLocation.Name;
                _editContext.NotifyFieldChanged(FieldIdentifier.Create(() => CurrentEvent.Location));

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

        public async ValueTask DisposeAsync()
        {
            _dotNetRef?.Dispose();
        }


        protected void OnDaySelectionChanged(DayTimeEntry entry, object? value)
        {
            entry.IsSelected = (bool)value;
        }
        protected bool IsValidTimeFormat(string? start)
        {
            return !string.IsNullOrWhiteSpace(start);
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
            GeneralFreeTextTime = null;
            _dateRange = null;
            DayTimeList.Clear();
            _timeError = string.Empty;
            _descriptionerror = string.Empty;
            _coverImageError = string.Empty;
            uploadedImage = null;
            SelectedLocationId = null;

            StateHasChanged();
        }
        protected async Task ShowConfirmation(string title, string description, string buttonTitle, Func<Task> onConfirmedAction)
        {
            var parameters = new DialogParameters
        {
            { "Title", title },
            { "Descrption", description },
            { "ButtonTitle", buttonTitle },
            { "OnConfirmed", EventCallback.Factory.Create(this, onConfirmedAction) }
        };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;

        }


        #region Custom Markdown Editor

        protected async Task<FileUploadResponse> FileUploadAsync(FileUploadModel fileUploadData)
        {
            try
            {
                var response = await FileUploadService.UploadFileAsync(fileUploadData);
                var jsonString = await response.Content.ReadAsStringAsync();
                if (response != null && response.IsSuccessStatusCode)
                {
                    FileUploadResponse? result = JsonSerializer.Deserialize<FileUploadResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new();
                }
                else if (response?.StatusCode == HttpStatusCode.BadRequest)
                {
                    Snackbar.Add($"Bad Request: {jsonString}", Severity.Error);
                }
                else if (response?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action", Severity.Error);
                }
                else if (response?.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error", Severity.Error);
                }

                return new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "FileUploadAsync");
                return new();
            }
        }

        protected void TriggerCustomImageUpload()
        {
            _markdownfileUploadRef.OpenFilePickerAsync();
        }

        protected Task OnCustomButtonClicked(MarkdownButtonEventArgs eventArgs)
        {
            if (eventArgs.Name is not null)
            {
                if (eventArgs.Name == UploadImageButtonName)
                {
                    TriggerCustomImageUpload();
                }

            }
            return Task.CompletedTask;
        }

        protected async Task UploadFile(string base64)
        {
            try
            {
                var fileUploadData = new FileUploadModel
                {
                    Container = BlobContainerName,
                    File = base64
                };

                var fileUploadResponse = await FileUploadAsync(fileUploadData);
                if (fileUploadResponse?.IsSuccess == true)
                {
                    var imageMarkdown = $"\n![image-{fileUploadResponse.FileName}]({fileUploadResponse.FileUrl})";
                    CurrentEvent.EventDescription += imageMarkdown;
                    await MarkdownEditorRef!.SetValueAsync(CurrentEvent.EventDescription);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UploadFile");
            }
        }

        protected async Task HandleMarkdownFilesChanged(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    using var stream = file.OpenReadStream(2 * 1024 * 1024); // 2MB limit
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var base64 = Convert.ToBase64String(memoryStream.ToArray());
                    var uploadedImageBase64 = $"data:{file.ContentType};base64,{base64}";
                    if (!string.IsNullOrWhiteSpace(uploadedImageBase64))
                    {
                        await UploadFile(uploadedImageBase64);
                    }
                    _markdownfileUploadRef?.ResetValidation();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleMarkdownFilesChanged");
            }
        }

        #endregion
    };
}

