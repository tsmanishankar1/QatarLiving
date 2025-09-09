using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Pages.Content.BackOffice.Daily.SelectionDialog;
using QLN.Web.Shared.Services.Interface;



namespace QLN.Web.Shared.Pages.Content.BackOffice
{
    public class DailyBackOfficeBase : ComponentBase
    {
        [Inject]
        public IDialogService DialogService { get; set; }
        [Inject] private ISimpleMemoryCache _simpleCacheService { get; set; }
        protected ContentsDailyPageResponse LandingContent { get; set; } = new ContentsDailyPageResponse();
        protected ContentPost TopStoryItem { get; set; } = new ContentPost();
        protected ContentPost SelectedItem { get; set; } = new ContentPost();
        protected ContentEvent HighlightedEvent { get; set; } = new ContentEvent();
        protected List<ContentEvent> vMoreArticles { get; set; } = [];
        protected List<ContentPost> TopStories { get; set; } = [];
        protected List<ContentEvent> TopicQueue1 { get; set; } = [];
        protected List<ContentEvent> TopicQueue2 { get; set; } = [];
        protected List<ContentEvent> TopicQueue3 { get; set; } = [];
        protected List<ContentEvent> TopicQueue4 { get; set; } = [];
        protected List<ContentEvent> TopicQueue5 { get; set; } = [];

        protected string TopicQueue1Label { get; set; } = string.Empty;
        protected string TopicQueue2Label { get; set; } = string.Empty;
        protected string TopicQueue3Label { get; set; } = string.Empty;
        protected string TopicQueue4Label { get; set; } = string.Empty;
        protected string TopicQueue5Label { get; set; } = string.Empty;


        public List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        protected async override Task OnInitializedAsync()
        {
            breadcrumbItems = new()
                {
                new() { Label = "Content", Url = $"/content/daily/backoffice", IsLast = true },
                };
            LandingContent = await _simpleCacheService.GetContentLandingAsync() ?? new();
            TopStoryItem = LandingContent?.ContentsDaily?.DailyTopStory?.Items.First() ?? new();
            HighlightedEvent = LandingContent?.ContentsDaily?.DailyEvent?.Items.First() ?? new();
            vMoreArticles = LandingContent?.ContentsDaily?.DailyMoreArticles?.Items ?? [];
            TopStories = LandingContent?.ContentsDaily?.DailyTopStories?.Items ?? [];
            TopicQueue1Label = LandingContent?.ContentsDaily?.DailyTopics1?.QueueLabel ?? "";
            TopicQueue2Label = LandingContent?.ContentsDaily?.DailyTopics2?.QueueLabel ?? "";
            TopicQueue3Label = LandingContent?.ContentsDaily?.DailyTopics3?.QueueLabel ?? "";
            TopicQueue4Label = LandingContent?.ContentsDaily?.DailyTopics4?.QueueLabel ?? "";
            TopicQueue1 = LandingContent?.ContentsDaily?.DailyTopics1?.Items ?? [];
            TopicQueue2 = LandingContent?.ContentsDaily?.DailyTopics2?.Items ?? [];
            TopicQueue3 = LandingContent?.ContentsDaily?.DailyTopics3?.Items ?? [];
            TopicQueue4 = LandingContent?.ContentsDaily?.DailyTopics4?.Items ?? [];
            TopicQueue5 = LandingContent?.ContentsDaily?.DailyTopics5?.Items ?? [];
            TopicQueue5Label = LandingContent?.ContentsDaily?.DailyTopics5?.QueueLabel ?? "";
        }
        public List<ContentEvent> Items { get; set; } = new List<ContentEvent>
    {
    new ContentEvent
    {
        CategroryId = "1",
        EntityOrganizer = "Qatar Events",
        EventCategory = "Sports",
        EventVenue = "Doha Stadium",
        EventStart = "2025-06-15T18:00:00Z",
        EventEnd = "2025-06-15T21:00:00Z",
        EventLat = "25.276987",
        EventLong = "51.520008",
        EventLocation = "Doha, Qatar",
        ImageUrl = "https://example.com/event.jpg",
        Slug = "qatar-sports-event",
        Title = "How to spot scam websites & social media accounts in Qatar",
        Category = "Sports"
    },
    new ContentEvent
    {
        CategroryId = "1",
        EntityOrganizer = "Qatar Events",
        EventCategory = "Sports",
        EventVenue = "Doha Stadium",
        EventStart = "2025-06-15T18:00:00Z",
        EventEnd = "2025-06-15T21:00:00Z",
        EventLat = "25.276987",
        EventLong = "51.520008",
        EventLocation = "Doha, Qatar",
        ImageUrl = "https://example.com/event.jpg",
        Slug = "qatar-sports-event",
        Title = "How to spot scam websites & social media accounts in Qatar",
         Category = "Sports"
    },
    new ContentEvent
    {
        CategroryId = "1",
        EntityOrganizer = "Qatar Events",
        EventCategory = "Sports",
        EventVenue = "Doha Stadium",
        EventStart = "2025-06-15T18:00:00Z",
        EventEnd = "2025-06-15T21:00:00Z",
        EventLat = "25.276987",
        EventLong = "51.520008",
        EventLocation = "Doha, Qatar",
        ImageUrl = "https://example.com/event.jpg",
        Slug = "qatar-sports-event",
        Title = "How to spot scam websites & social media accounts in Qatar",
        Category = "Event"
    },
    };
        protected Task OpenDialogAsync()
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            return DialogService.ShowAsync<SelectionDialog>(string.Empty, options);
        }
    };



}
