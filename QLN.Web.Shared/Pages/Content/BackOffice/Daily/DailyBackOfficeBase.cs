using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using MudBlazor;
using QLN.Web.Shared.Pages.Content.BackOffice.Daily.SelectionDialog;



namespace QLN.Web.Shared.Pages.Content.BackOffice
{
    public class DailyBackOfficeBase : ComponentBase
    {
        [Inject]
        public IDialogService DialogService { get; set; }

        public List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        protected async override Task OnInitializedAsync()
        {
            breadcrumbItems = new()
                {
                new() { Label = "Content", Url = $"/content/daily/backoffice", IsLast = true },
                };
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
                CloseOnEscapeKey = true,

            };
            return DialogService.ShowAsync<SelectionDialog>(string.Empty, options);
        }
    };
    

    
}
