using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.EventsPage
{
    public class EventsBase : QLComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        [Inject] IEventsService eventsService { get; set; }
        [Inject] ILogger<EventCreateFormBase> Logger { get; set; }
        protected List<EventCategoryModel> Categories = [];
        [Inject]
        public IDialogService DialogService { get; set; }
        protected List<EventDTO> featuredEvents = [];
        protected List<FeaturedSlot> featuredEventSlots = [];
        protected FeaturedSlot ReplaceSlot { get; set; } = new();
        protected EventDTO ReplacedEvent { get; set; } = new();
        protected EventDTO SelectedEvent { get; set; } = new();
        protected int activeIndex = 0;
        protected int index = 0;
        protected int currentIndex = 1;
        protected string searchText;
        protected string selectedType;
        protected List<string> categories = new List<string> { "All Events", "Featured Events" };
        protected PaginatedEventResponse PaginatedData { get; set; } = new();
        protected List<EventDTO> events => PaginatedData.Items;
        protected int currentPage = 1;
        public List<EventDTO> AllEventsList { get; set; } = new();
        protected int pageSize = 12;
        protected bool IsLoading = true;
        protected bool IsLoadingEvent = true;

        protected async Task HandlePageChange(int newPage)
        {
            currentPage = newPage;
            PaginatedData = await GetEvents(currentPage, pageSize);
            StateHasChanged();
        }
        private async Task HandleSearch(string value)
        {
            searchText = value;
            currentPage = 1;
            PaginatedData = await GetEvents(currentPage, pageSize, searchText);
            StateHasChanged();
        }
        protected int? currentStatus = 1;

        protected async Task HandleStatusChange(int? status)
        {
            currentStatus = status;
            currentPage = 1;
            PaginatedData = await GetEvents(currentPage, pageSize, searchText, SortAscending ? "asc" : "desc", currentStatus);
            StateHasChanged();
        }

        protected async Task HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1;
            PaginatedData = await GetEvents(currentPage, pageSize);
            StateHasChanged();
        }

        protected bool SortAscending = true;

        protected async Task HandleSortOrderChange(bool ascending)
        {
            SortAscending = ascending;
            PaginatedData = await GetEvents(currentPage, pageSize, searchText, SortAscending ? "asc" : "desc");
            StateHasChanged();
        }

        protected override async Task OnInitializedAsync()
        {
            await HandleStatusChange(1);
            featuredEventSlots = await GetFeaturedSlotsAsync();
            Categories = await GetEventsCategories();
            var allEvents = await GetAllEvents();
            var featuredEventTitles = featuredEventSlots
                .Where(slot => slot.Event != null)
                .Select(slot => slot.Event.EventTitle)
                .ToHashSet(StringComparer.OrdinalIgnoreCase); // for case-insensitive comparison

            AllEventsList = allEvents
                .Where(e => !featuredEventTitles.Contains(e.EventTitle))
                .ToList();
        }
        protected EventDTO? draggedItem;

        protected void OnDragStart(EventDTO item)
        {
            draggedItem = item;
        }

        protected void OnDrop(EventDTO targetItem)
        {
            if (draggedItem != null && draggedItem != targetItem)
            {
                var fromIndex = events.IndexOf(draggedItem);
                var toIndex = events.IndexOf(targetItem);

                events.Remove(draggedItem);
                events.Insert(toIndex, draggedItem);

                draggedItem = null;
            }
        }
        protected void NavigateToAddEvent()
        {
            Navigation.NavigateTo("/content/events/create");
        }
        protected Task OpenDialogAsync()
        {
            var parameters = new DialogParameters
            {
                { nameof(MessageBoxBase.Title), "Featured Event" },
                { nameof(MessageBoxBase.Placeholder), "Article Title*" },
                { nameof(MessageBoxBase.events), AllEventsList },
                { nameof(MessageBoxBase.OnAdd), EventCallback.Factory.Create<FeaturedSlot>(this, HandleEventSelected) }
            };
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true
            };
            return DialogService.ShowAsync<MessageBox>("", parameters, options);
        }
        protected async Task HandleEventSelected(FeaturedSlot selectedEvent)
        {
            if (ReplaceSlot?.SlotNumber > 0 && ReplaceSlot?.SlotNumber != null)
            {
                var targetSlot = featuredEventSlots.FirstOrDefault(s => s.SlotNumber == ReplaceSlot?.SlotNumber);
                if (targetSlot != null)
                {
                    targetSlot.Event = selectedEvent.Event;
                    ReplacedEvent = selectedEvent.Event;
                    ReplacedEvent.FeaturedSlot.Id = ReplaceSlot?.SlotNumber ?? 0;
                    ReplacedEvent.IsFeatured = true;
                    ReplaceFeaturedEvent();
                }
            }
            await Task.CompletedTask;
        }
        protected async Task ReplaceEventSlot(FeaturedSlot selectedEvent)
        {
            ReplaceSlot = selectedEvent;
            OpenDialogAsync();
            await Task.CompletedTask;
        }
        protected async Task UpdateEvent(EventDTO selectedEvent)
        {
            SelectedEvent = selectedEvent;

            await Task.CompletedTask;
        }
        protected List<PostItem> _posts = Enumerable.Range(1, 12).Select(i => new PostItem
        {
            Number = i,
            PostTitle = "Family Residence Visa status stuck “Under Review”",
            Category = i == 2 ? "Missing home" : "Advise and help",
            Username = "Ismat Zerin",
            CreationDate = new DateTime(2025, 4, 12),
            Reporter = "Ismat Zerin",
            ReportDate = new DateTime(2025, 4, 12),
            LiveFor = "2 hours"
        }).ToList();

        protected void DeletePost(int number)
        {
            _posts.RemoveAll(p => p.Number == number);
        }
        public class PostItem
        {
            public int Number { get; set; }
            public string PostTitle { get; set; } = "";
            public string Category { get; set; } = "";
            public string Username { get; set; } = "";
            public DateTime CreationDate { get; set; }
            public string Reporter { get; set; } = "";
            public DateTime ReportDate { get; set; }
            public string LiveFor { get; set; } = "";
        }
        protected Status status = Status.Live;

        protected Color GetButtonColor(Status s) => s == status ? Color.Warning : Color.Default;

        protected enum Status
        {
            Live,
            Published,
            Unpublished
        }
        private async Task<PaginatedEventResponse> GetEvents(
              int page = 1,
              int pageSize = 12,
              string search = "",
              string sortOrder = "desc",
              int? status = null
          )
        {
            try
            {
                IsLoading = true;
                var apiResponse = await eventsService.GetEventsByPagination(
                    page: page,
                    perPage: pageSize,
                    search: search ?? "",
                    categoryId: null,
                    sortOrder: sortOrder,
                    fromDate: null,
                    toDate: null,
                    filterType: "",
                    status: status,
                    location: null,
                    freeOnly: false,
                    featuredFirst: false
                );

                if (apiResponse.IsSuccessStatusCode)
                {
                    var result = await apiResponse.Content.ReadFromJsonAsync<PaginatedEventResponse>();
                    if (result != null)
                    {
                        result.Items = result.Items
                            .Where(e => e.IsActive)
                            .ToList();
                    }
                    return result ?? new PaginatedEventResponse();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching paginated events");
            }
            finally
            {
                IsLoading = false;
            }

            return new PaginatedEventResponse();
        }

        private async Task<List<FeaturedSlot>> GetFeaturedSlotsAsync()
        {
            IsLoadingEvent = true;
            var slots = Enumerable.Range(1, 6)
                .Select(i => new FeaturedSlot
                {
                    SlotNumber = i,
                    Event = new EventDTO
                    {
                        EventTitle = "Feature an Event"
                    }
                }).ToList();

            try
            {
                var apiResponse = await eventsService.GetFeaturedEvents();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var events = await apiResponse.Content.ReadFromJsonAsync<List<EventDTO>>() ?? new();
                    var rawContent = await apiResponse.Content.ReadAsStringAsync();
                    foreach (var ev in events)
                    {
                        if (ev.FeaturedSlot != null && ev.FeaturedSlot.Id >= 1 && ev.FeaturedSlot.Id <= 6)
                        {
                            var index = ev.FeaturedSlot.Id - 1;
                            slots[index] = new FeaturedSlot
                            {
                                SlotNumber = ev.FeaturedSlot.Id,
                                Event = ev
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while fetching featured slots");
            }
            finally
            {
                IsLoadingEvent = false;
            }
            return slots;
        }
        private async Task<List<EventDTO>> GetFeaturedvents()
        {
            try
            {
                var apiResponse = await eventsService.GetFeaturedEvents();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<List<EventDTO>>();
                    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    return response ?? new List<EventDTO>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsLocations");
            }
            return new List<EventDTO>();
        }
        private async Task<List<EventDTO>> GetAllEvents()
        {
            try
            {
                var apiResponse = await eventsService.GetAllEvents();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<List<EventDTO>>();
                    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    return response ?? new List<EventDTO>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEvents");
            }
            return new List<EventDTO>();
        }

        private async Task<List<EventCategoryModel>> GetEventsCategories()
        {
            try
            {
                var apiResponse = await eventsService.GetEventCategories();
                if (apiResponse.IsSuccessStatusCode)
                {
                    return await apiResponse.Content.ReadFromJsonAsync<List<EventCategoryModel>>() ?? [];
                }
                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsCategories");
                return [];
            }
        }
        protected async Task DeleteEvent(string eventId)
        {
            try
            {
                var apiResponse = await eventsService.DeleteEvent(eventId);
                if (apiResponse.IsSuccessStatusCode)
                {
                    Snackbar.Add("Event deleted successfully", Severity.Success);
                    index = 0;
                    PaginatedData = await GetEvents(currentPage, pageSize, searchText, SortAscending ? "asc" : "desc", currentStatus);
                    StateHasChanged();
                }
                else
                {
                    Snackbar.Add("Failed to delete event", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteEvent");
                Snackbar.Add("Something went wrong while deleting the event.", Severity.Error);
            }
        }
        protected async Task DeleteFeatureEvent(string eventId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eventId))
                {
                    Snackbar.Add("Invalid event ID.", Severity.Warning);
                    return;
                }
                var slot = featuredEventSlots.FirstOrDefault(s => s.Event?.Id.ToString() == eventId);
                if (slot == null)
                {
                    Snackbar.Add("No featured event found with the given ID.", Severity.Warning);
                    return;
                }
                var apiResponse = await eventsService.DeleteEvent(eventId);
                if (apiResponse.IsSuccessStatusCode)
                {
                    slot.Event = new EventDTO
                    {
                        EventTitle = "Feature an Event"
                    };
                    Snackbar.Add("Featured event deleted successfully", Severity.Success);
                    featuredEventSlots = await GetFeaturedSlotsAsync();
                }
                else
                {
                    Snackbar.Add("Failed to delete featured event.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteFeatureEvent");
                Snackbar.Add("Something went wrong while deleting the featured event.", Severity.Error);
            }
        }
        protected async Task ReplaceFeaturedEvent()
        {
            try
            {
                var payload = new
                {
                    eventId = ReplacedEvent.Id,
                    isFeatured = true,
                    slot = new
                    {
                        id = ReplacedEvent.FeaturedSlot.Id,
                        name = ReplacedEvent.FeaturedSlot.Name
                    }
                };

                var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                Logger.LogInformation("New slot replacement payload: {Payload}", payloadJson);

                var apiResponse = await eventsService.UpdateFeaturedEvents(payload);
                if (apiResponse.IsSuccessStatusCode)
                {
                    Snackbar.Add("Event Replaced successfully", Severity.Success);
                    PaginatedData = await GetEvents(currentPage, pageSize, searchText, SortAscending ? "asc" : "desc", currentStatus);
                    StateHasChanged();
                }
                else
                {
                    var error = await apiResponse.Content.ReadAsStringAsync();
                    Logger.LogError("Failed to replace event. StatusCode: {StatusCode}, Response: {Response}", apiResponse.StatusCode, error);
                    Snackbar.Add("Failed to replace event", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ReplaceFeaturedEvent");
                Snackbar.Add("Something went wrong while replacing the event.", Severity.Error);
            }
        }

        protected async Task UpdateEvent()
        {
            try
            {
                var apiResponse = await eventsService.UpdateEvents(SelectedEvent);
                if (apiResponse.IsSuccessStatusCode)
                {
                    Snackbar.Add("Event  successfully", Severity.Success);
                    PaginatedData = await GetEvents(currentPage, pageSize, searchText, SortAscending ? "asc" : "desc", currentStatus);
                    StateHasChanged();
                }
                else
                {
                    Snackbar.Add("Failed to delete event", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteEvent");
                Snackbar.Add("Something went wrong while deleting the event.", Severity.Error);
            }
        }
        protected async Task UpdateEventStatusAsync((EventDTO evt, EventStatus newStatus) args)
        {
            var (evt, newStatus) = args;
            evt.Status = newStatus;
            var response = await eventsService.UpdateEvents(evt);
            if (response.IsSuccessStatusCode)
            {
                if (evt.Status == EventStatus.Published)
                {
                    Snackbar.Add($"Event Published Successfully!", Severity.Success);
                }
                else if (evt.Status == EventStatus.UnPublished)
                {
                    Snackbar.Add($"Event UnPublished Successfully!", Severity.Success);
                }
                PaginatedData = await GetEvents(currentPage, pageSize, searchText, SortAscending ? "asc" : "desc", currentStatus);
            }
            else
            {
                Snackbar.Add($"Failed to update status to {newStatus}.", Severity.Error);
            }
        }
        protected async Task DeleteEventOnClick(string id)
        {
        var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Descrption", "Do you want to delete this Article?" },
            { "ButtonTitle", "Delete" },
            { "OnConfirmed",  EventCallback.Factory.Create(this, async () => await DeleteFeatureEvent(id))}
        };
        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
        var result = await dialog.Result;
    }
    }
}