using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components.EventListCard;

namespace QLN.Web.Shared.Pages.Content.Events.EventsList
{
    public class EventListBase : ComponentBase
    {
        [Parameter] public List<ContentEvent> Items { get; set; } = [];
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 12;
        protected string SelectedSort { get; set; } = "default";
        [Inject] protected NavigationManager Navigation { get; set; }

        public class SortOption
        {
            public string Id { get; set; }
            public string Label { get; set; }
        }

        protected List<SortOption> SortOptions = new()
    {
        new SortOption { Id = "default", Label = "Default" },
        new SortOption { Id = "high_to_low", Label = "Price: High to Low" },
        new SortOption { Id = "low_to_high", Label = "Price: Low to High" }
    };

        protected List<EventListCard.EventItem> EventItems = new()
    {
        new EventListCard.EventItem
        {
            Title = "FIFA Arab Cup 2025",
            Description = "The FIFA Arab Cup brings together the best of Arab football, showcasing...",
            Category = "Sports",
            Location = "West Bay, Doha",
            PriceMin = 250,
            PriceMax = 1500,
            ImageUrl = "/images/event_image.svg",
            StartDate = new DateTime(2025, 12, 12),
            EndDate = new DateTime(2026, 1, 13)
        },
        new EventListCard.EventItem
        {
            Title = "Web Summit Qatar",
            Description = "Tech event showcasing innovation and entrepreneurship in Doha.",
            Category = "Technology",
            Location = "West Bay, Doha",
            PriceMin = 250,
            PriceMax = 1500,
            ImageUrl = "/images/event_image.svg",
            StartDate = new DateTime(2025, 3, 12),
            EndDate = new DateTime(2025, 4, 13)
        },
        new EventListCard.EventItem
        {
            Title = "International Book Fair 2025",
            Description = "Explore a variety of books and meet global publishers in Doha.",
            Category = "Education",
            Location = "West Bay, Doha",
            PriceMin = 210,
            PriceMax = 1500,
            ImageUrl = "/images/event_image.svg",
            StartDate = new DateTime(2025, 6, 12),
            EndDate = new DateTime(2025, 7, 13)
        },
        new EventListCard.EventItem
        {
            Title = "International Book Fair 2025",
            Description = "Explore a variety of books and meet global publishers in Doha.",
            Category = "Education",
            Location = "West Bay, Doha",
            PriceMin = 310,
            PriceMax = 2500,
            ImageUrl = "/images/event_image.svg",
            StartDate = new DateTime(2025, 6, 12),
            EndDate = new DateTime(2025, 7, 13)
        }
    };

        protected IEnumerable<EventListCard.EventItem> FilteredEventItems => SelectedSort switch
        {
            "high_to_low" => EventItems.OrderByDescending(e => e.PriceMax),
            "low_to_high" => EventItems.OrderBy(e => e.PriceMin),
            _ => EventItems
        };

        protected IEnumerable<ContentEvent> PagedFilteredEventItems =>
                Items
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);

        protected Task OnSortChanged(string newSort)
        {
            SelectedSort = newSort;
            CurrentPage = 1;
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected void HandleCardClick(ContentEvent item)
        {
          Navigation.NavigateTo($"/events/details/{item.Slug}");
        }
        protected void HandlePageChange(int newPage)
        {
            CurrentPage = newPage;
            StateHasChanged();
        }

        protected void HandlePageSizeChange(int newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            StateHasChanged();
        }
    }
}
