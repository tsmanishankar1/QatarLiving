using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class MoreArticleBOCardBase : ComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }
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
    }
};
  [Parameter]
    public bool isLoading { get; set; } = false;

    protected void NavigatetoArticle()
    {
        NavigationManager.NavigateTo("content/events");
    }

    protected void NavigatetoArticle(ContentEvent article)
    {
        NavigationManager.NavigateTo($"/content/article/details/{article.Slug}");
    }

}