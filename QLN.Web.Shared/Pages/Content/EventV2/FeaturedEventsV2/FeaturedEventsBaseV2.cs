using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services;

namespace QLN.Web.Shared.Pages.Content.EventV2.FeaturedEventsV2
{
    public class FeaturedEventsBaseV2 : ComponentBase
    {
        [Parameter]
        public List<EventDTOV2> FeaturedEvent { get; set; } = new();

        [Parameter]
        public bool Loading { get; set; } = true;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        private NavigationManager Navigation { get; set; } = default!;

        private bool _categoriesSwiperInitialized = false;
        [Inject]
        private IOptions<NavigationPath> NavigationPath { get; set; } = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_categoriesSwiperInitialized && !Loading && FeaturedEvent?.Any() == true)
            {
                _categoriesSwiperInitialized = true;
                await JSRuntime.InvokeVoidAsync("initCategoriesSwiper");
            }
        }

        protected void HandleCategoryClick(EventDTOV2 clickedItem)
        {
            Navigation.NavigateTo($"{NavigationPath.Value.ContentEventsDetail}{clickedItem.Slug}", true);
        }

        protected List<EventDTOV2> GetSlotOrderedEvents(List<EventDTOV2> events)
        {
            const int maxSlots = 100;
            var slotEvents = new EventDTOV2[maxSlots];

            foreach (var ev in events)
            {
                if (ev.IsFeatured && ev.FeaturedSlot != null && ev.FeaturedSlot.Id > 0 && ev.FeaturedSlot.Id <= maxSlots)
                {
                    int index = ev.FeaturedSlot.Id - 1;
                    slotEvents[index] = ev;
                }
            }

            return slotEvents.Where(e => e != null).ToList();
        }

    }
}