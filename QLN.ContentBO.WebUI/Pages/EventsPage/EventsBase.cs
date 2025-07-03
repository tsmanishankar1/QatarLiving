using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;
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
        protected List<EventDTO> events = [];
        protected int activeIndex = 0;
        protected int index = 0;
        protected int currentIndex = 1;
        protected string searchText;
        protected string selectedType;
        protected List<string> categories = new List<string> { "All Events", "Featured Events" };
        protected override async Task OnInitializedAsync()
        {
            events = await GetEvents();
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
    }
}