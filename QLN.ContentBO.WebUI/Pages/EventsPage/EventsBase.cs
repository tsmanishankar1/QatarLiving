using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
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
        protected List<EventDTO> events = [];
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
        protected override async Task OnInitializedAsync()
        {
            events = await GetEvents();
            featuredEventSlots = await GetFeaturedSlotsAsync();
            Categories = await GetEventsCategories();
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
        private async Task<List<EventDTO>> GetEvents()
        {
            try
            {
                var apiResponse = await eventsService.GetAllEvents();
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
                    events = await GetEvents();
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
                    events = await GetEvents();
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