using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.Events.EventsList
{
    public class EventListBase : ComponentBase
    {
        [Parameter] public List<ContentEvent> Items { get; set; } = [];
        [Parameter]
        public bool Loading { get; set; } = false;
        protected string SelectedSort { get; set; } = "default";

        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        [Parameter] public int CurrentPage { get; set; }
        [Parameter] public int PageSize { get; set; }
        [Parameter] public int TotalItems { get; set; }

        protected IEnumerable<ContentEvent> FilteredEventItems => Items; // Apply filtering here if needed
        protected IEnumerable<ContentEvent> PagedFilteredEventItems => FilteredEventItems
                    .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);
        protected async void HandlePageChange(int newPage)
        {
            await OnPageChange.InvokeAsync(newPage);
        }

        protected async void HandlePageSizeChange(int newSize)
        {
            await OnPageSizeChange.InvokeAsync(newSize);
        }

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


        protected void HandleCardClick(ContentEvent item)
        {
            Navigation.NavigateTo($"/content/events/details/{item.Slug}");
        }

    }
}
