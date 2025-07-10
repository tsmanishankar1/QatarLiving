using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components.EventListCard;

namespace QLN.Web.Shared.Pages.Content.EventV2.EventsListV2
{
    public class EventListBaseV2 : ComponentBase
    {
        [Parameter] public List<EventDTOV2> Items { get; set; } = [];

        [Parameter] public bool Loading { get; set; } = false;

        private string _selectedSort = "default";

        [Parameter]
        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (_selectedSort != value)
                {
                    _selectedSort = value;
                    _ = OnSortChanged.InvokeAsync(value); // Fire event when changed
                }
            }
        }

        [Parameter] public EventCallback<string> OnSortChanged { get; set; }

        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
        [Parameter] public int CurrentPage { get; set; }
        [Parameter] public int PageSize { get; set; }
        [Parameter] public int TotalItems { get; set; }

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

        protected void HandleCardClick(EventDTOV2 item)
        {
          
            Navigation.NavigateTo($"/content/v2/events/details/{item.Slug}");
        }
    }
}
