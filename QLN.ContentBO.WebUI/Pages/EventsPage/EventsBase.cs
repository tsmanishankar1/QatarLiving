using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components;

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
        protected int activeIndex = 0;
        protected int index = 0;
        protected int currentIndex = 1;
        protected string searchText;
        protected string selectedType;
        protected List<string> categories = new List<string> { "All Events", "Featured Events" };
        protected PaginatedEventResponse PaginatedData { get; set; } = new(); 
        protected List<EventDTO> events => PaginatedData.Items;
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected bool IsLoading = true;

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
            currentPage = 1; // Reset to first page when page size changes
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
            foreach (var ev in Categories)
            {
                Console.WriteLine($"{ev.Id}, StartDate: {ev.CategoryName}");
            }
            foreach (var ev in events)
            {
                Console.WriteLine($"Event Title: {ev.EventTitle}, CategoryId: {ev.CategoryId}, StartDate: {ev.EventSchedule.StartDate}");
            }
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
                { nameof(MessageBoxBase.events), events },
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
                    Console.WriteLine($"Replaced event in slot {selectedEvent.SlotNumber} with: {selectedEvent.Event.EventTitle}");
                }
            }
            await Task.CompletedTask;
        }
        protected async Task ReplaceEventSlot(FeaturedSlot selectedEvent)
        {
            Console.WriteLine($"Selected event: {selectedEvent?.Event?.EventTitle}");
            ReplaceSlot = selectedEvent;
            OpenDialogAsync();
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
        protected void OnPlaceholderClick(FeaturedSlot slot)
        {

            Console.WriteLine($"Slot Clicked: {slot.SlotNumber}, Title: {slot.Event?.EventTitle}");
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
                    categoryId: null, // or use your real filter logic
                    sortOrder: sortOrder,
                    fromDate: null,
                    toDate: null,
                    filterType: "", // "Live", "Published", etc.
                    status: status,
                    location: null,
                    freeOnly: false,
                    featuredFirst: false
                );

                if (apiResponse.IsSuccessStatusCode)
                {
                    var result = await apiResponse.Content.ReadFromJsonAsync<PaginatedEventResponse>();
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
                    return response ?? new List<EventDTO>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsLocations");
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
        protected async Task ReplaceFeaturedEvent()
        {
            try
            {
                var apiResponse = await eventsService.UpdateFeaturedEvents(ReplacedEvent);
                if (apiResponse.IsSuccessStatusCode)
                {
                    
                    Snackbar.Add("Event Replaced successfully", Severity.Success);
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
    }
}