using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components.EventListCard;

namespace QLN.Web.Shared.Pages.Content.Events.EventsList
{
    public class EventListBase : ComponentBase
    {
        [Parameter] public List<ContentEvent> Items { get; set; } = [];
         [Parameter]
    public bool Loading { get; set; } = false;
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
 protected IEnumerable<ContentEvent> FilteredEventItems => Items ?? Enumerable.Empty<ContentEvent>();

        protected IEnumerable<ContentEvent> PagedFilteredEventItems =>
            FilteredEventItems
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);

     protected Task OnSortChanged(string newSort)
        {
            Console.WriteLine($"Sort option changed to: {newSort}");
            SelectedSort = newSort;
            CurrentPage = 1;
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected void HandleCardClick(ContentEvent item)
        {
          Navigation.NavigateTo($"/content/events/details/{item.Slug}");
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
